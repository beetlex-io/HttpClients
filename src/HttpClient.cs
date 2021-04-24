using BeetleX.Clients;
using BeetleX.Tasks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http.Headers;
using System.Net.Security;
using System.Reflection;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeetleX.Http.Clients
{

    public class HttpClientHandlerPool
    {
        public HttpClientHandlerPool(Uri uri)
        {
            mUri = uri;
            Host = uri.Host;
            Port = uri.Port;
            TimeOut = 5000;
            MaxConnections = 100;
            Clients = new List<HttpClientHandler>();
            SSL = uri.Scheme.IndexOf("https", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private Uri mUri;

        public bool SSL { get; set; }

        public string Host { get; set; }

        public int Port { get; set; }

        public int MaxWaitLength { get; set; } = 20;

        private Stack<HttpClientHandler> mPools = new Stack<HttpClientHandler>();

        private Queue<TaskCompletionSource<HttpClientHandler>> mWaitQueue = new Queue<TaskCompletionSource<HttpClientHandler>>();

        public List<HttpClientHandler> Clients { get; private set; }

        public int MaxConnections { get; set; }

        public int Connections => Clients.Count;

        public int TimeOut { get; set; }

        public SslProtocols? SslProtocols { get; set; } = System.Security.Authentication.SslProtocols.Tls | System.Security.Authentication.SslProtocols.Tls11 | System.Security.Authentication.SslProtocols.Tls12;

        public RemoteCertificateValidationCallback CertificateValidationCallback { get; set; }

        public Task<HttpClientHandler> Pop()
        {
            lock (this)
            {
                if (mPools.Count > 0)
                {
                    var result = mPools.Pop();
                    result.Using = true;
                    result.TimeOut = BeetleX.TimeWatch.GetElapsedMilliseconds() + TimeOut;
                    return Task.FromResult(result);
                }
                if (Clients.Count < MaxConnections)
                {
                    var result = Create();
                    result.Using = true;
                    result.TimeOut = BeetleX.TimeWatch.GetElapsedMilliseconds() + TimeOut;
                    return Task.FromResult(result);
                }
                if (mWaitQueue.Count < MaxWaitLength)
                {
                    TaskCompletionSource<HttpClientHandler> completionSource = new TaskCompletionSource<HttpClientHandler>();
                    mWaitQueue.Enqueue(completionSource);
                    return completionSource.Task;
                }
                else
                {
                    throw new HttpClientException($"Request {Host} connections limit");
                }

            }
        }

        public void Push(HttpClientHandler client)
        {
            TaskCompletionSource<HttpClientHandler> result = null;
            lock (this)
            {
                if (mWaitQueue.Count > 0)
                {
                    result = mWaitQueue.Dequeue();
                    client.Using = true;
                    client.TimeOut = BeetleX.TimeWatch.GetElapsedMilliseconds() + TimeOut;
                }
                else
                {
                    client.Using = false;
                    mPools.Push(client);
                }
            }

            if (result != null)
            {
                Task.Run(() => result.SetResult(client));
            }
        }

        private HttpClientHandler Create()
        {
            var packet = new HttpClientPacket();
            AsyncTcpClient client;
            if (SSL)
            {
                client = SocketFactory.CreateSslClient<AsyncTcpClient>(packet, Host, Port, Host);
                if (CertificateValidationCallback != null)
                    client.CertificateValidationCallback = CertificateValidationCallback;
                else
                    client.CertificateValidationCallback = (o, e, d, f) => true;
            }
            else
            {
                client = SocketFactory.CreateClient<AsyncTcpClient>(packet, Host, Port);
            }
            if (this.SslProtocols != null)
                client.SslProtocols = this.SslProtocols;
            packet.Client = client;
            client.Connected = c =>
            {
                c.Socket.NoDelay = true;
                c.Socket.ReceiveTimeout = TimeOut;
                c.Socket.SendTimeout = TimeOut;
            };
            var result = new HttpClientHandler();
            result.Using = false;
            result.Client = client;
            lock (Clients)
                Clients.Add(result);
            return result;
        }

    }

    public class HttpClientHandler
    {
        public IClient Client { get; set; }

        public HttpClientHandlerPool Pool { get; set; }

        public long TimeOut { get; set; }

        public bool Using { get; set; }

    }

    public class HttpClientPoolFactory
    {

        private static System.Collections.Concurrent.ConcurrentDictionary<string, HttpClientHandlerPool> mPools
            = new System.Collections.Concurrent.ConcurrentDictionary<string, HttpClientHandlerPool>(StringComparer.OrdinalIgnoreCase);

        public static System.Collections.Concurrent.ConcurrentDictionary<string, HttpClientHandlerPool> Pools => mPools;

        public static void SetPoolInfo(Uri host, int maxConn, int timeout)
        {
            HttpClientHandlerPool pool = GetPool(null, host);
            pool.MaxConnections = maxConn;
            pool.TimeOut = timeout;
        }

        public static void SetPoolInfo(string host, int maxConn, int timeout)
        {
            SetPoolInfo(new Uri(host), maxConn, timeout);
        }

        public static HttpClientHandlerPool GetPool(string key, Uri uri)
        {
            if (string.IsNullOrEmpty(key))
                key = $"{uri.Host}:{uri.Port}";
            HttpClientHandlerPool result;
            if (mPools.TryGetValue(key, out result))
                return result;
            return CreatePool(key, uri);
        }

        private static HttpClientHandlerPool CreatePool(string key, Uri uri)
        {
            lock (typeof(HttpClientPoolFactory))
            {
                HttpClientHandlerPool result;
                if (!mPools.TryGetValue(key, out result))
                {
                    result = new HttpClientHandlerPool(uri);
                    mPools[key] = result;
                }
                return result;
            }
        }
    }

    public class HttpHost
    {

        private static System.Collections.Concurrent.ConcurrentDictionary<string, HttpHost> mHostPool
            = new System.Collections.Concurrent.ConcurrentDictionary<string, HttpHost>(StringComparer.OrdinalIgnoreCase);

        public static HttpHost GetHttpHost(Uri host)
        {
            return GetHttpHost(host.ToString());
        }
        public static HttpHost GetHttpHost(string host)
        {
            if (!mHostPool.TryGetValue(host, out HttpHost result))
            {
                result = new HttpHost(host);
                mHostPool[host] = result;
            }
            return result;
        }

        protected HttpHost(string host) : this(new Uri(host))
        {

        }

        protected HttpHost(Uri host)
        {
            this.Uri = host;
            Formater = new FormUrlFormater();
            Host = this.Uri.Host;
            Port = this.Uri.Port;
            mPoolKey = $"{this.Uri.Host}:{this.Uri.Port}";
            mPool = HttpClientPoolFactory.GetPool(mPoolKey, this.Uri);
            Available = true;
            InVerify = false;
        }

        private HttpClientHandlerPool mPool;

        public long ID { get; set; }

        private long mSuccess;

        private long mLastSuccess;

        private long mError;

        private int mSocketErrors;

        public int Weight { get; set; }

        public HttpClientHandlerPool Pool => mPool;

        private string mPoolKey;

        public static int DisconnectErrors { get; set; } = 5;

        public string Host { get; set; }

        public int Port { get; set; }

        public IBodyFormater Formater { get; set; }

        public Uri Uri { get; private set; }

        public bool Available { get; set; }

        public long Success => mSuccess;

        public long Error => mError;

        internal int SocketErrors => mSocketErrors;

        internal bool InVerify { get; set; }

        public int MaxRPS { get; set; }

        internal void AddSuccess()
        {
            mSuccess++;
            Available = true;
        }

        internal void AddError(bool socketError)
        {
            if (socketError)
            {
                mSocketErrors++;

            }
            else
            {
                mSocketErrors = 0;

            }
            if (mSocketErrors >= DisconnectErrors)
                Available = false;
            else
                Available = true;
            mError++;
        }


        public override string ToString()
        {
            string result = $"{mSuccess - mLastSuccess}/{mSuccess}";
            mLastSuccess = mSuccess;
            return result;
        }

        public Request Put(string url, object data)
        {
            return Put(url, data, new FormUrlFormater(), null);
        }



        public Request Put(string url, object data, IBodyFormater formater, Type bodyType = null)
        {
            return Put(url, null, null, data, formater, bodyType);
        }

        public Request Put(string url, Dictionary<string, string> header, Dictionary<string, string> queryString, object data, IBodyFormater formater, Type bodyType = null)
        {
            Request request = new Request();
            request.Method = Request.PUT;
            request.Formater = formater == null ? this.Formater : formater;
            request.Header = new Header();
            request.Header[HeaderTypeFactory.CONTENT_TYPE] = request.Formater.ContentType;
            request.Header[HeaderTypeFactory.HOST] = Host;
            if (header != null)
                foreach (var item in header)
                    request.Header[item.Key] = item.Value;
            request.QuestryString = queryString;
            request.Url = url;
            request.Body = data;
            request.BodyType = bodyType;
            request.HttpHost = this;
            return request;
        }


        public Request Post(string url, object data)
        {
            return Post(url, null, null, data, new FormUrlFormater(), null);
        }

        public Request Post(string url, object data, IBodyFormater formater, Type bodyType = null)
        {
            return Post(url, null, null, data, formater, bodyType);
        }

        public Request Post(string url, Dictionary<string, string> header, Dictionary<string, string> queryString, object data, IBodyFormater formater, Type bodyType = null)
        {
            Request request = new Request();
            request.Method = Request.POST;
            request.Formater = formater == null ? this.Formater : formater;
            request.Header = new Header();

            request.Header[HeaderTypeFactory.CONTENT_TYPE] = request.Formater.ContentType;
            request.Header[HeaderTypeFactory.HOST] = Host;
            if (header != null)
                foreach (var item in header)
                    request.Header[item.Key] = item.Value;
            request.QuestryString = queryString;
            request.Url = url;
            request.Body = data;
            request.BodyType = bodyType;
            request.HttpHost = this;
            return request;
        }

        public Request Delete(string url)
        {
            return Delete(url, new FormUrlFormater(), null);
        }
        public Request Delete(string url, IBodyFormater formater, Type bodyType = null)
        {
            return Delete(url, null, null, formater, bodyType);
        }

        public Request Delete(string url, Dictionary<string, string> header, Dictionary<string, string> queryString, IBodyFormater formater, Type bodyType = null)
        {
            Request request = new Request();
            request.Header = new Header();
            request.Formater = formater == null ? this.Formater : formater;
            request.Method = Request.DELETE;
            request.Header[HeaderTypeFactory.HOST] = Host;
            request.QuestryString = queryString;
            if (header != null)
                foreach (var item in header)
                    request.Header[item.Key] = item.Value;
            request.Url = url;
            request.BodyType = bodyType;
            request.HttpHost = this;
            return request;
        }
        public Request Get(string url)
        {
            return Get(url, null, null, new FormUrlFormater(), null);
        }
        public Request Get(string url, IBodyFormater formater, Type bodyType = null)
        {
            return Get(url, null, null, formater, bodyType);
        }
        public Request Get(string url, Dictionary<string, string> header, Dictionary<string, string> queryString, IBodyFormater formater, Type bodyType = null)
        {
            Request request = new Request();
            request.Header = new Header();
            request.Formater = formater == null ? this.Formater : formater;
            //request.Header[HeaderTypeFactory.CONTENT_TYPE] = request.Formater.ContentType;
            request.Header[HeaderTypeFactory.HOST] = Host;
            request.QuestryString = queryString;
            if (header != null)
                foreach (var item in header)
                    request.Header[item.Key] = item.Value;
            request.Url = url;
            request.BodyType = bodyType;
            request.HttpHost = this;
            return request;
        }

    }

    public class HttpBinaryClient : HttpClient<BinaryFormater>
    {
        public HttpBinaryClient(string host) : base(host)
        {

        }
    }


    public class HttpFormUrlClient : HttpClient<FormUrlFormater>
    {
        public HttpFormUrlClient(string host) : base(host)
        {

        }
    }

    public class HttpJsonClient : HttpClient<JsonFormater>
    {
        public HttpJsonClient(string host) : base(host)
        {

        }
    }

    public class HttpFormDataClient : HttpClient<FromDataFormater>
    {
        public HttpFormDataClient(string host) : base(host)
        {

        }
    }

    public class HttpClient<T>
        where T : IBodyFormater, new()
    {
        public HttpClient(string host)
        {
            mHost = HttpHost.GetHttpHost(host);
        }

        private HttpHost mHost;

        private Dictionary<string, string> mQueryString = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private Dictionary<string, string> mHeader = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        private object mDataObject;

        private Dictionary<string, object> mDataMap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        public HttpClient<T> Accept(string value)
        {
            mHeader["accept"] = value;
            return this;
        }

        public HttpClient<T> Authorization(string value)
        {
            mHeader["authorization"] = value;
            return this;
        }

        public HttpClient<T> SetHeader(string name, string value)
        {
            mHeader[name] = value;
            return this;
        }

        public HttpClient<T> AddQueryString(string name, object value)
        {
            mQueryString[name] = value.ToString();
            return this;
        }

        public HttpClient<T> SetBody(object data)
        {
            mDataObject = data;
            return this;
        }

        public HttpClient<T> AddBodyFile(string name, string file)
        {

            AddBodyField(name, new FileInfo(file));
            return this;
        }

        public HttpClient<T> AddBodyFile(string name, UploadFile file)
        {
            AddBodyField(name, file);
            return this;
        }

        public HttpClient<T> AddBodyField(string name, object data)
        {
            mDataMap[name] = data;
            return this;
        }
        public async Task<RESULT> Get<RESULT>(string url)
        {
            var response = await Get(url, typeof(RESULT));
            return response.GetResult<RESULT>();
        }
        public Task<Response> Get(string url, Type bodyType = null)
        {
            var request = mHost.Get(url, mHeader, mQueryString, new T(), bodyType);
            return request.Execute();
        }
        public async Task<RESULT> Post<RESULT>(string url)
        {
            var response = await Post(url, typeof(RESULT));
            return response.GetResult<RESULT>();
        }
        public Task<Response> Post(string url, Type bodyType = null)
        {
            var request = mHost.Post(url, mHeader, mQueryString, mDataObject == null ? mDataMap : mDataObject, new T(), bodyType);
            return request.Execute();
        }
        public async Task<RESULT> Put<RESULT>(string url)
        {
            var response = await Put(url, typeof(RESULT));
            return response.GetResult<RESULT>();
        }
        public Task<Response> Put(string url, Type bodyType = null)
        {
            var request = mHost.Put(url, mHeader, mQueryString, mDataObject == null ? mDataMap : mDataObject, new T(), bodyType);
            return request.Execute();
        }
        public async Task<RESULT> Delete<RESULT>(string url)
        {
            var response = await Delete(url, typeof(RESULT));
            return response.GetResult<RESULT>();
        }
        public Task<Response> Delete(string url, Type bodyType = null)
        {
            var request = mHost.Delete(url, mHeader, mQueryString, new T(), bodyType);
            return request.Execute();
        }
    }

}
