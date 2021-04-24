using BeetleX.Clients;
using BeetleX.Tasks;
using System;
using System.Collections.Generic;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
#if NETCOREAPP2_1
using BeetleX.Tracks;
#endif

namespace BeetleX.Http.WebSockets
{
    public class WSClient : IDisposable
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

        private long mSends = 0;

        private long mReceives = 0;

        public int TimeOut { get; set; } = 10 * 1000;

        private Dictionary<string, object> mProperties = new Dictionary<string, object>();

        private System.Collections.Concurrent.ConcurrentQueue<object> mDataFrames = new System.Collections.Concurrent.ConcurrentQueue<object>();

        private bool OnWSConnected = false;

        private void OnPacketCompleted(IClient client, object message)
        {
            if (message is DataFrame dataFrame)
            {
                OnReceiveMessage(dataFrame);
            }
            else
            {
                OnConnectResponse(null, message as Response);
            }
        }


        public object Token { get; set; }

        public object this[string name]
        {
            get
            {
                mProperties.TryGetValue(name, out object result);
                return result;
            }
            set
            {
                mProperties[name] = value;
            }
        }


        public AsyncTcpClient Client => mNetClient;

        public DateTime PingPongTime { get; set; }

        public event System.EventHandler<WSReceiveArgs> DataReceive;

        public void Ping()
        {
            DataFrame pong = new DataFrame();
            pong.Type = DataPacketType.ping;
            SendFrame(pong);
        }

        public virtual byte[] GetFrameDataBuffer(int length)
        {
            return new byte[length];
        }

        public virtual void FreeFrameDataBuffer(byte[] data)
        {

        }

        internal void FrameWrited(DataFrame frame)
        {
            OnDataFrameWrited(frame);
        }



        protected virtual void OnDataFrameWrited(DataFrame frame)
        {

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
                    e.Error = new BXException($"ws client receive error {e_.Message}", e_);
                    DataReceive?.Invoke(this, e);
                }
                catch { }
            }
        }

        protected virtual void OnReceiveMessage(DataFrame message)
        {
            System.Threading.Interlocked.Increment(ref mReceives);
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
                    SendFrame(pong);
                }
                return;
            }
            else
            {
                OnDataReceive(message);
            }

        }

        private object mLockReceive = new object();

        protected virtual void OnDataReceive(DataFrame data)
        {
            if (DataReceive != null)
            {
                WSReceiveArgs e = new WSReceiveArgs();
                e.Client = this;
                e.Frame = data;
                DataReceive(this, e);
            }
            else
            {
                lock (mLockReceive)
                {
                    if (mReceiveCompletionSource != null)
                    {
                        var result = mReceiveCompletionSource;
                        mReceiveCompletionSource = null;
                        Task.Run(() => result.Success(data));
                    }
                    else
                    {
                        mDataFrames.Enqueue(data);
                    }
                }
            }
        }

        private IAnyCompletionSource mReceiveCompletionSource;

        private void OnReceiveTimeOut(IAnyCompletionSource source)
        {
            if (mReceiveCompletionSource != null)
            {
                var completed = mReceiveCompletionSource;
                mReceiveCompletionSource = null;
                Task.Run(() =>
                {
                    completed?.Error(new BXException("Websocket receive time out!"));
                });
                return;
            }
        }

        public Task<DataFrame> ReceiveFrame()
        {
            Connect();
            lock (mLockReceive)
            {
                if (mDataFrames.TryDequeue(out object data))
                {
                    if (data is Exception error)
                        throw error;
                    return Task.FromResult((DataFrame)data);
                }
                else
                {
                    mReceiveCompletionSource = CompletionSourceFactory.Create<DataFrame>(TimeOut);
                    mReceiveCompletionSource.TimeOut = OnReceiveTimeOut;
                    return (Task<DataFrame>)mReceiveCompletionSource.GetTask();
                }
            }

        }
        private void OnClientError(IClient c, ClientErrorArgs e)
        {
            if (OnWSConnected)
            {
                if (e.Error is BXException)
                {
                    OnWSConnected = false;
                }
                lock (mLockReceive)
                {
                    if (mReceiveCompletionSource != null)
                    {
                        var completed = mReceiveCompletionSource;
                        mReceiveCompletionSource = null;
                        Task.Run(() =>
                        {
                            completed.Error(e.Error);
                        });
                        return;
                    }
                }

                if (DataReceive != null)
                {
                    try
                    {
                        WSReceiveArgs wse = new WSReceiveArgs();
                        wse.Client = this;
                        wse.Error = e.Error;
                        DataReceive?.Invoke(this, wse);
                    }
                    catch { }

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

        public EndPoint LocalEndPoint { get; set; }

        private object mLockConnect = new object();

        public bool IsConnected => OnWSConnected && mNetClient != null && mNetClient.IsConnected;

        public void Connect()
        {
            if (IsConnected)
            {
                return;
            }
            lock (mLockConnect)
            {
                if (IsConnected)
                {
                    return;
                }
#if NETCOREAPP2_1
                using (CodeTrackFactory.Track($"Connect {Host}", CodeTrackLevel.Function, null, "Websocket", "Client"))
                {
#endif
                    mWScompletionSource = new TaskCompletionSource<bool>();
                    if (mNetClient == null)
                    {
                        string protocol = Host.Scheme.ToLower();
                        if (!(protocol == "ws" || protocol == "wss"))
                        {
                            OnConnectResponse(new BXException("protocol error! host must [ws|wss]//host:port"), null);
                            mWScompletionSource.Task.Wait();
                        }
                        WSPacket wSPacket = new WSPacket
                        {
                            WSClient = this
                        };
                        if (Host.Scheme.ToLower() == "wss")
                        {
                            mNetClient = SocketFactory.CreateSslClient<AsyncTcpClient>(wSPacket, Host.Host, Host.Port, SSLAuthenticateName);
                            mNetClient.CertificateValidationCallback = CertificateValidationCallback;
                        }
                        else
                        {
                            mNetClient = SocketFactory.CreateClient<AsyncTcpClient>(wSPacket, Host.Host, Host.Port);
                        }
                        mNetClient.LocalEndPoint = this.LocalEndPoint;
                        mNetClient.LittleEndian = false;
                        mNetClient.PacketReceive = OnPacketCompleted;
                        mNetClient.ClientError = OnClientError;
                    }
                    mDataFrames = new System.Collections.Concurrent.ConcurrentQueue<object>();
                    bool isNew;
                    if (mNetClient.Connect(out isNew))
                    {
                        OnWriteConnect();
                    }
                    else
                    {
                        OnConnectResponse(mNetClient.LastError, null);
                    }
                    mWScompletionSource.Task.Wait(10000);
                    if (!OnWSConnected)
                        throw new TimeoutException($"Connect {Host} websocket server timeout!");
#if NETCOREAPP2_1
                }
#endif
            }
        }

        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        protected virtual void OnConnectResponse(Exception exception, Response response)
        {
            Response = response;
            Task.Run(() =>
            {
                if (exception != null)
                {
                    OnWSConnected = false;
                    mWScompletionSource?.TrySetException(exception);
                }
                else
                {
                    if (response.Code == 101)
                    {
                        OnWSConnected = true;
                        mWScompletionSource?.TrySetResult(true);

                    }
                    else
                    {
                        OnWSConnected = false;
                        mWScompletionSource?.TrySetException(new BXException($"ws connect error {response.Code} {response.Message}"));

                    }
                }
            });
        }

        public void SendFrame(DataFrame data)
        {
            Connect();
            data.Client = this;
            data.MaskKey = this.MaskKey;
            mNetClient.Send(data);
            System.Threading.Interlocked.Increment(ref mSends);
        }

        public void Dispose()
        {
            OnWSConnected = false;
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
