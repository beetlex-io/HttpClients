#if NETCOREAPP2_1
using BeetleX.Tracks;
#endif
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
namespace BeetleX.Http.WebSockets
{
    public class JsonClient : WSClient
    {
        public JsonClient(string host) : base(host) { }

        public JsonClient(Uri host) : base(host) { }

        public virtual void Send(object data)
        {
#if NETCOREAPP2_1
            using (CodeTrackFactory.Track("Send", CodeTrackLevel.Function, null, "Websocket", "JsonClient"))
            {
#endif
                string text = Newtonsoft.Json.JsonConvert.SerializeObject(data);
                DataFrame df = new DataFrame();
                var buffer = Encoding.UTF8.GetBytes(text);
                df.Body = new ArraySegment<byte>(buffer, 0, buffer.Length);
                base.SendFrame(df);
#if NETCOREAPP2_1
            }
#endif
        }


        public virtual async Task<JToken> Receive()
        {
#if NETCOREAPP2_1
            using (CodeTrackFactory.Track("Receive", CodeTrackLevel.Function, null, "Websocket", "JsonClient"))
            {
#endif
                var data = await ReceiveFrame();
                if (data.Type != DataPacketType.text)
                    throw new BXException("Data type is not json text");
                if (data.Body == null)
                    return new JObject();
                var body = data.Body.Value;
                string result = Encoding.UTF8.GetString(body.Array, body.Offset, body.Count);
                return (JToken)Newtonsoft.Json.JsonConvert.DeserializeObject(result);
#if NETCOREAPP2_1
            }
#endif
        }

        public async Task<JToken> ReceiveFrom(object data)
        {
#if NETCOREAPP2_1
            using (CodeTrackFactory.Track("Request", CodeTrackLevel.Function, null, "Websocket", "JsonClient"))
            {
#endif
                Send(data);
                return await Receive();
#if NETCOREAPP2_1
            }
#endif
        }

    }
}
