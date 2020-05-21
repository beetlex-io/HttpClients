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
            string text = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            DataFrame df = new DataFrame();
            var buffer = Encoding.UTF8.GetBytes(text);
            df.Body = new ArraySegment<byte>(buffer, 0, buffer.Length);
            base.SendFrame(df);
        }


        public virtual async Task<JToken> Receive()
        {
            var data = await ReceiveFrame();
            if (data.Type != DataPacketType.text)
                throw new BXException("Data type is not json text");
            if (data.Body == null)
                return new JObject();
            var body = data.Body.Value;
            string result = Encoding.UTF8.GetString(body.Array, body.Offset, body.Count);
            return (JToken)Newtonsoft.Json.JsonConvert.DeserializeObject(result);
        }

        public async Task<JToken> ReceiveFrom(object data)
        {
            Send(data);
            return await Receive();
        }

    }
}
