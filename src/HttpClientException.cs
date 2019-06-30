using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.Clients
{
    public class HttpClientException : Exception
    {
        public HttpClientException(Request request, Uri host, string message, Exception innerError = null) : base($"request {host} error {message}", innerError)
        {
            Request = request;
            Host = host;
            SocketError = false;
            if (innerError != null && (innerError is System.Net.Sockets.SocketException || innerError is ObjectDisposedException))
            {
                SocketError = true;
            }
        }

        public int Code { get; internal set; }

        public Uri Host { get; internal set; }

        public Request Request { get; internal set; }

        public bool SocketError { get; internal set; }

    }
}
