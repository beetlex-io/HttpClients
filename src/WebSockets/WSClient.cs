using BeetleX.Clients;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleX.Http.WebSockets
{
    public abstract class WSClient : IDisposable
    {

        public WSClient(string host) : this(new Uri(host)) { }

        public WSClient(Uri host)
        {
            Host = host;
            byte[] key = new byte[16];
            new Random().NextBytes(key);
            _SecWebSocketKey = Convert.ToBase64String(key);
            this.Origin = Host.OriginalString;
            this.SSLAuthenticateName = this.Origin;
        }

        public Response Response { get; internal set; }

        public byte[] MaskKey { get; set; }

        private string _SecWebSocketKey;

        private AsyncTcpClient mNetClient;

        private bool OnSocketConnected = false;

        private Queue<DataFrame> mReceiveMessages = new Queue<DataFrame>();

        private void OnPacketCompleted(IClient client, object message)
        {
            if (message is DataFrame dataFrame)
            {
                OnPushReceiveMessage(dataFrame);
            }
            else
            {
                OnConnectResponse(null, message as Response);
            }
        }

        public DateTime PingPongTime { get; set; }

        public event System.EventHandler<WSReceiveArgs> DataReceive;

        public async Task Ping()
        {
            DataFrame pong = new DataFrame();
            pong.Type = DataPacketType.ping;
            await Send(pong);
        }

        protected virtual void OnDataReceive(WSReceiveArgs e)
        {
            try
            {
                DataReceive?.Invoke(this, e);
            }
            catch (Exception e_)
            {
                try
                {
                    e.Message = new BXException($"ws client receive error {e_.Message}", e_);
                    DataReceive?.Invoke(this, e);
                }
                catch { }
            }
        }

        protected virtual void OnPushReceiveMessage(DataFrame message)
        {
            if (message.Type == DataPacketType.connectionClose)
            {
                Dispose();
                OnClientError(mNetClient, new ClientErrorArgs { Error = new BXException("ws connection close!"), Message = "ws connection close" });
                return;
            }
            if (message.Type == DataPacketType.ping || message.Type == DataPacketType.pong)
            {
                PingPongTime = DateTime.Now;
                if (message.Type == DataPacketType.ping)
                {
                    DataFrame pong = new DataFrame();
                    pong.Type = DataPacketType.pong;
                    Send(pong);
                }
                return;
            }
            lock (mReceiveMessages)
            {
                if (mReceiveDompletionSource != null)
                {
                    var result = mReceiveDompletionSource;
                    mReceiveDompletionSource = null;
                    Task.Run(() =>
                    {
                        result.TrySetResult(message);                  
                    });

                }
                else
                {
                    mReceiveMessages.Enqueue(message);
                }
            }

        }

        private TaskCompletionSource<DataFrame> mReceiveDompletionSource;

        private (DataFrame, Task<DataFrame>) OnPopReceiveMessage()
        {
            lock (mReceiveMessages)
            {
                (DataFrame, Task<DataFrame>) result = default;
                if (mReceiveMessages.Count > 0)
                {
                    result.Item1 = mReceiveMessages.Dequeue();
                }
                else
                {
                    mReceiveDompletionSource = new TaskCompletionSource<DataFrame>();
                    result.Item2 = mReceiveDompletionSource.Task;
                }

                return result;
            }
        }

        private void OnClientError(IClient c, ClientErrorArgs e)
        {
            if (OnSocketConnected)
            {
                if (e.Error is BXException)
                {
                    OnSocketConnected = false;
                }
                if (mReceiveDompletionSource != null)
                {
                    Task.Run(() =>
                    {
                        mReceiveDompletionSource.TrySetException(e.Error);
                        mReceiveDompletionSource = null;
                    });
                }
            }
            else
            {
                OnConnectResponse(e.Error, null);
            }

        }

        private TaskCompletionSource<bool> mWScompletionSource;

        private void OnWriteConnect()
        {
            var stream = mNetClient.Stream.ToPipeStream();
            stream.WriteLine($"{Method} {Path} HTTP/1.1");
            stream.WriteLine($"Host: {Host.Host}");
            stream.WriteLine($"Upgrade: websocket");
            stream.WriteLine($"Connection: Upgrade");
            foreach (var item in Headers)
            {
                stream.WriteLine($"{item.Key}: {item.Value}");
            }
            stream.WriteLine($"Origin: {Origin}");
            stream.WriteLine($"Sec-WebSocket-Key: {_SecWebSocketKey}");
            stream.WriteLine($"Sec-WebSocket-Version: {SecWebSocketVersion}");
            stream.WriteLine("");
            mNetClient.Stream.Flush();
        }

        private object mLockConnect = new object();

        private Task<bool> OnConnect()
        {
            if (!OnSocketConnected || mNetClient == null || !mNetClient.IsConnected)
            {
                lock (mLockConnect)
                {
                    if (OnSocketConnected && mNetClient == null && !mNetClient.IsConnected)
                    {

                        return Task.FromResult(true);
                    }
                    mWScompletionSource = new TaskCompletionSource<bool>();
                    if (mNetClient == null)
                    {
                        string protocol = Host.Scheme.ToLower();
                        if (!(protocol == "ws" || protocol == "wss"))
                        {
                            OnConnectResponse(new BXException("protocol error! host must [ws|wss]//host:port"), null);
                            return mWScompletionSource.Task;
                        }
                        WSPacket wSPacket = new WSPacket
                        {
                            WSClient = this
                        };
                        if (Host.Scheme.ToLower() == "wss")
                        {
                            mNetClient = SocketFactory.CreateSslClient<AsyncTcpClient>(wSPacket, Host.Host, Host.Port, SSLAuthenticateName);
                        }
                        else
                        {
                            mNetClient = SocketFactory.CreateClient<AsyncTcpClient>(wSPacket, Host.Host, Host.Port);
                        }
                        mNetClient.LittleEndian = false;
                        mNetClient.PacketReceive = OnPacketCompleted;
                        mNetClient.ClientError = OnClientError;
                    }
                    mReceiveMessages.Clear();
                    if (mNetClient.Connect())
                    {
                        OnWriteConnect();
                    }
                    else
                    {
                        OnConnectResponse(mNetClient.LastError, null);
                    }
                    return mWScompletionSource.Task;
                }
            }
            else
            {
                return Task.FromResult(OnSocketConnected);
            }
        }

        protected virtual void OnConnectResponse(Exception exception, Response response)
        {      
            Response = response;
            Task.Run(() =>
            {
                if (exception != null)
                {
                    OnSocketConnected = false;
                    mWScompletionSource?.TrySetException(exception);
                }
                else
                {
                    if (response.Code != 101)
                    {
                        OnSocketConnected = false;
                        mWScompletionSource?.TrySetException(new BXException($"ws connect error {response.Code} {response.Message}"));

                    }
                    else
                    {
                        Open();
                        OnSocketConnected = true;  
                        mWScompletionSource?.TrySetResult(true);
                        

                    }
                }
                mWScompletionSource = null;
            });
        }

        private void Open()
        {
            Task.Run(async () => {
                while (true)
                {
                    WSReceiveArgs args = new WSReceiveArgs();
                    try
                    {
                        var frame = await Receive();
                        args.Client = this;
                        args.Frame = frame;
                    }
                    catch (Exception e_)
                    {
                        args.Error = e_;
                        break;
                    }
                    OnDataReceive(args);
                }
            });
        }

        private async Task<DataFrame> Receive()
        {
            await OnConnect();
            var result = OnPopReceiveMessage();
            if (result.Item1 != null)
            {
                return result.Item1;
            }
            else
            {
                var df = await result.Item2;
                return df;
            }

        }

        protected async Task Send(DataFrame data)
        {
            await OnConnect();
            data.MaskKey = this.MaskKey;
            mNetClient.Send(data);

        }

        public void Dispose()
        {
            OnSocketConnected = false;
            mNetClient.DisConnect();
            mNetClient = null;
        }

        public Dictionary<string, string> Headers { get; private set; } = new Dictionary<string, string>();

        public string SecWebSocketProtocol { get; set; } = "websocket, beetlex";

        public int SecWebSocketVersion { get; set; } = 13;

        public string Method { get; private set; } = "GET";

        public string Path { get; set; } = "/";

        public string SSLAuthenticateName { get; set; }

        public string Origin { get; set; }

        public Uri Host { get; private set; }
    }
}
