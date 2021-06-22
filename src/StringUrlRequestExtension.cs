using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.Clients
{
    public static class StringUrlRequestExtension
    {
        public static HttpClient<T> GetRequest<T>(this string url)
            where T : IBodyFormater, new()
        {
            return new HttpClient<T>(url);
        }

        public static HttpBinaryClient BinaryRequest(this string url)
        {
            return new HttpBinaryClient(url);
        }
        public static HttpJsonClient JsonRequest(this string url)
        {
            return new HttpJsonClient(url);
        }

        public static HttpFormUrlClient FormUrlRequest(this string url)
        {
            return new HttpFormUrlClient(url);
        }

        public static HttpFormDataClient FormDataRequest(this string url)
        {
            return new HttpFormDataClient(url);
        }
    }
}
