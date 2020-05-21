using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace BeetleX.Http.WebSockets
{
    public class TextClient : WSClient
    {
        public TextClient(string host) : base(host) { }

        public TextClient(Uri host) : base(host) { }

        public virtual void Send(string text)
        {
            DataFrame dataFrame = new DataFrame();
            byte[] data = Encoding.UTF8.GetBytes(text);
            dataFrame.Body = new ArraySegment<byte>(data, 0, data.Length);
            SendFrame(dataFrame);
        }

        public virtual async Task<string> Receive()
        {
            var data = await ReceiveFrame();
            if (data.Type != DataPacketType.text)
                throw new BXException("Data type is not text");
            if (data.Body == null)
                return null;
            var body = data.Body.Value;
            string result = Encoding.UTF8.GetString(body.Array, body.Offset, body.Count);
            return result;
        }

        public async Task<string> ReceiveFrom(string text)
        {
            Send(text);
            return await Receive();
        }
    }
}
