﻿using BeetleX.Tasks;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Http.Clients
{

    public class HttpInterfaceProxy : System.Reflection.DispatchProxy
    {
        public HttpInterfaceProxy()
        {
            TimeOut = 10000;
        }

        public HttpHost Host { get; set; }

        public int TimeOut { get; set; }

        protected override object Invoke(MethodInfo targetMethod, object[] args)
        {
            ClientActionHanler handler = ClientActionFactory.GetHandler((MethodInfo)targetMethod);
            var rinfo = handler.GetRequestInfo(args);
            var host = handler.Host != null ? handler.Host : Host;
            if (host == null)
                throw new Exception("The service host is not defined!");
            var request = rinfo.GetRequest(host);
            request.TimeOut = TimeOut;
            var task = request.Execute();
            if (!handler.Async)
            {
                throw new HttpClientException(request, host.Uri, $"{targetMethod.Name} method invoke not supported, the return value must be task!");
            }
            else
            {
                IAnyCompletionSource source = CompletionSourceFactory.Create(handler.ReturnType, TimeOut);
                source.Wait<Response>(task, (c, t) =>
                {
                    if (t.Result.Exception != null)
                    {
                        c.Error(t.Result.Exception);
                    }
                    else
                    {
                        c.Success(t.Result.Body);
                    }
                });
                return source.GetTask();
            }
        }
    }

    public class HttpApiClient
    {
        internal HttpApiClient(string host)
        {
            Host = HttpHost.GetHttpHost(host); //new HttpHost(host);
        }

        private static Dictionary<string, HttpApiClient> mClients = new Dictionary<string, HttpApiClient>(StringComparer.OrdinalIgnoreCase);

        public static T Create<T>(string host, int timeout = 10000)
        {
            lock (typeof(HttpApiClient))
            {
                if (!mClients.TryGetValue(host, out HttpApiClient client))
                {
                    client = new HttpApiClient(host);
                    client.TimeOut = timeout;
                }
                object result;
                result = client.Create<T>();
                ((HttpInterfaceProxy)result).TimeOut = timeout;
                return (T)result;
            }
        }

        public static T Create<T>(int timeout = 10000)
        {
            object result;
            result = DispatchProxy.Create<T, HttpInterfaceProxy>();
            ((HttpInterfaceProxy)result).TimeOut = timeout;
            return (T)result;
        }

        public int TimeOut { get; set; } = 10000;

        public HttpHost Host { get; set; }

        protected async Task<T> OnExecute<T>(MethodBase targetMethod, params object[] args)
        {
            var rinfo = ClientActionFactory.GetHandler((MethodInfo)targetMethod).GetRequestInfo(args);
            var request = rinfo.GetRequest(Host);
            var respnse = await request.Execute();
            if (respnse.Exception != null)
                throw respnse.Exception;
            return (T)respnse.Body;
        }

        private System.Collections.Concurrent.ConcurrentDictionary<Type, object> mAPI = new System.Collections.Concurrent.ConcurrentDictionary<Type, object>();

        public T Create<T>()
        {
            Type type = typeof(T);
            object result;
            if (!mAPI.TryGetValue(type, out result))
            {
                result = DispatchProxy.Create<T, HttpInterfaceProxy>();

                mAPI[type] = result;
                ((HttpInterfaceProxy)result).Host = Host;
                ((HttpInterfaceProxy)result).TimeOut = TimeOut;
            }
            return (T)result;
        }
    }
}
