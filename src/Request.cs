using BeetleX.Buffers;
using BeetleX.Clients;
using BeetleX.Tasks;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
#if NETCOREAPP2_1
using BeetleX.Tracks;
#endif
namespace BeetleX.Http.Clients
{
    public enum LoadedState : int
    {
        None = 1,
        Method = 2,
        Header = 4,
        Completed = 8
    }

#if NETCOREAPP2_1
    public class Request : IApiObject
#else
    public class Request
#endif
    {
#if NETCOREAPP2_1

        private CodeTrack mRequestTrack;

        public string Name => this.Url;

        public string[] Group
        {
            get
            {
                return new[] { "HTTPClient", "Request", $"{HttpHost.Host}" };
            }
        }
#endif

        public const string CODE_TREAK_PARENTID = "TrackParentID";

        public const string POST = "POST";

        public const string GET = "GET";

        public const string DELETE = "DELETE";

        public const string PUT = "PUT";

        public Request()
        {
            Method = GET;
            this.HttpProtocol = "HTTP/1.1";
        }

        public IBodyFormater Formater { get; set; } = new JsonFormater();

        public Dictionary<string, string> QuestryString { get; set; }

        public Header Header { get; set; }

        public string Url { get; set; }

        public string Method { get; set; }

        public string HttpProtocol { get; set; }

        public Response Response { get; set; }

        public Object Body { get; set; }

        public Type BodyType { get; set; }

        public int? TimeOut { get; set; }

        public RequestStatus Status { get; set; }

        public IClient Client { get; set; }

        internal void Execute(PipeStream stream)
        {
            var buffer = HttpParse.GetByteBuffer();
            int offset = 0;
            offset += Encoding.ASCII.GetBytes(Method, 0, Method.Length, buffer, offset);
            buffer[offset] = HeaderTypeFactory._SPACE_BYTE;
            offset++;
            offset += Encoding.ASCII.GetBytes(Url, 0, Url.Length, buffer, offset);
            if (QuestryString != null && QuestryString.Count > 0)
            {
                int i = 0;
                foreach (var item in this.QuestryString)
                {
                    string key = item.Key;
                    string value = item.Value;
                    if (string.IsNullOrEmpty(value))
                        continue;
                    value = System.Net.WebUtility.UrlEncode(value);
                    if (i == 0)
                    {
                        buffer[offset] = HeaderTypeFactory._QMARK;
                        offset++;
                    }
                    else
                    {
                        buffer[offset] = HeaderTypeFactory._AND;
                        offset++;
                    }
                    offset += Encoding.ASCII.GetBytes(key, 0, key.Length, buffer, offset);
                    buffer[offset] = HeaderTypeFactory._EQ;
                    offset++;
                    offset += Encoding.ASCII.GetBytes(value, 0, value.Length, buffer, offset);
                    i++;
                }
            }
            buffer[offset] = HeaderTypeFactory._SPACE_BYTE;
            offset++;
            offset += Encoding.ASCII.GetBytes(HttpProtocol, 0, HttpProtocol.Length, buffer, offset);
            buffer[offset] = HeaderTypeFactory._LINE_R;
            offset++;
            buffer[offset] = HeaderTypeFactory._LINE_N;
            offset++;
            stream.Write(buffer, 0, offset);
            if (Header != null)
                Header.Write(stream);
            if (Method == POST || Method == PUT)
            {
                if (Body != null)
                {
                    stream.Write(HeaderTypeFactory.CONTENT_LENGTH_BYTES, 0, 16);
                    MemoryBlockCollection contentLength = stream.Allocate(10);
                    stream.Write(HeaderTypeFactory.TOW_LINE_BYTES, 0, 4);
                    int len = stream.CacheLength;
                    Formater.Serialization(Body, stream);
                    int count = stream.CacheLength - len;
                    contentLength.Full(count.ToString().PadRight(10), stream.Encoding);
                }
                else
                {
                    stream.Write(HeaderTypeFactory.NULL_CONTENT_LENGTH_BYTES, 0, HeaderTypeFactory.NULL_CONTENT_LENGTH_BYTES.Length);
                    stream.Write(HeaderTypeFactory.LINE_BYTES, 0, 2);
                }
            }
            else
            {
                stream.Write(HeaderTypeFactory.LINE_BYTES, 0, 2);
            }
        }

        public HttpHost HttpHost { get; set; }

        public Action<AsyncTcpClient> GetConnection { get; set; }

        public static event Action<Request> Executing;

        public async Task<Response> Execute()
        {
#if NETCOREAPP2_1
            using (mRequestTrack = CodeTrackFactory.TrackReport(this, CodeTrackLevel.Module, null))
            {
                if (Activity.Current != null)
                    Header[CODE_TREAK_PARENTID] = Activity.Current.Id;
                if (mRequestTrack.Enabled)
                    mRequestTrack.Activity?.AddTag("tag", "BeetleX HttpClient");
#endif
                Executing?.Invoke(this);
                mTaskCompletionSource = CompletionSourceFactory.Create<Response>(TimeOut != null ? TimeOut.Value : HttpHost.Pool.TimeOut); // new AnyCompletionSource<Response>();
                mTaskCompletionSource.TimeOut = OnRequestTimeout;
                OnExecute();
                return await (Task<Response>)mTaskCompletionSource.GetTask();
#if NETCOREAPP2_1
            }
#endif
        }

        private IAnyCompletionSource mTaskCompletionSource;

        private void OnRequestTimeout(IAnyCompletionSource source)
        {
            Response response = new Response();
            response.Code = "408";
            response.CodeMsg = "Request timeout";
            response.Exception = new HttpClientException(this, HttpHost.Uri, "Request timeout");
            source?.Success(response);
        }

        private void onEventClientError(IClient c, ClientErrorArgs e)
        {
            requestResult.TrySetResult(e.Error);
        }

        private void OnEventClientPacketCompleted(IClient client, object message)
        {
            requestResult.TrySetResult(message);
        }

        private TaskCompletionSource<object> requestResult;

        private async void OnExecute()
        {
            HttpClientHandler client = null;
            Response response;
            try
            {
                object result = null;
                client = await HttpHost.Pool.Pop();
                Client = client.Client;
                requestResult = new TaskCompletionSource<object>();
                AsyncTcpClient asyncClient = (AsyncTcpClient)client.Client;
                asyncClient.ClientError = onEventClientError;
                asyncClient.PacketReceive = OnEventClientPacketCompleted;
                GetConnection?.Invoke(asyncClient);
#if NETCOREAPP2_1
                using (CodeTrackFactory.Track(Url, CodeTrackLevel.Function, mRequestTrack?.Activity?.Id, "HTTPClient", "Protocol", "Write"))
                {
                    asyncClient.Send(this);
                    Status = RequestStatus.SendCompleted;
                }
#else
                    asyncClient.Send(this);
                    Status = RequestStatus.SendCompleted;
#endif

#if NETCOREAPP2_1
                using (CodeTrackFactory.Track(Url, CodeTrackLevel.Function, mRequestTrack?.Activity?.Id, "HTTPClient", "Protocol", "Read"))
                {
                    var a = requestResult.Task;
                    result = await a;
                }
#else
                    var a = requestResult.Task;
                    result = await a;
#endif
                if (result is Exception error)
                {
                    response = new Response();
                    response.Exception = new HttpClientException(this, HttpHost.Uri, error.Message, error);
                    Status = RequestStatus.Error;
                }
                else
                {
                    response = (Response)result;
                    Status = RequestStatus.Received;
                }

                if (response.Exception == null)
                {
                    int code = int.Parse(response.Code);
                    if (response.Length > 0)
                    {
                        try
                        {
                            if (code == 200)
                                response.Body = this.Formater.Deserialization(response, response.Stream, this.BodyType, response.Length);
                            else
                                response.Body = response.Stream.ReadString(response.Length);
                        }
                        finally
                        {
                            response.Stream.ReadFree(response.Length);
                            if (response.Chunked)
                                response.Stream.Dispose();
                            response.Stream = null;
                        }
                    }
                    if (!response.KeepAlive)
                        client.Client.DisConnect();
                    if (code != 200)
                    {
                        response.Exception = new HttpClientException(this, HttpHost.Uri, $"{Url}({response.Code}) [{response.Body}]");
                        response.Exception.Code = code;
                    }
                    Status = RequestStatus.Completed;
                }
            }
            catch (Exception e_)
            {
                HttpClientException clientException = new HttpClientException(this, HttpHost.Uri, e_.Message, e_);
                response = new Response { Exception = clientException };
                Status = RequestStatus.Error;
            }
            if (response.Exception != null)
                HttpHost.AddError(response.Exception.SocketError);
            else
                HttpHost.AddSuccess();
            Response.Current = response;
            this.Response = response;
            if (client != null)
            {
                if (client.Client is AsyncTcpClient asclient)
                {
                    asclient.ClientError = null;
                    asclient.PacketReceive = null;
                }
                HttpHost.Pool.Push(client);
            }
            await Task.Run(() => mTaskCompletionSource.Success(response));
        }
    }

    public enum RequestStatus
    {
        None,
        SendCompleted,
        Received,
        Completed,
        Error
    }
}
