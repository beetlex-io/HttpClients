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

        protected override void OnDataReceive(WSReceiveArgs e)
        {
            var message = e.Frame;
            if (message.Type == DataPacketType.text)
            {
                if (message.Length > 0)
                {
                    e.Message = Encoding.UTF8.GetString(message.Body, 0, message.Body.Length);
                }
                else
                {
                    e.Message = null;
                }
            }
            else
            {
                e.Error = new BXException($"ws receive data type is {message.Type}");
            }
            if (mReceiveCompletionSource != null)
            {
                var rec = mReceiveCompletionSource;
                mReceiveCompletionSource = null;
                Task.Run(() =>
                {
                    if (e.Error != null)
                    {
                        rec.TrySetException(e.Error);
                    }
                    else
                    {
                        rec.TrySetResult((string)e.Message);
                    }
                });
            }
            else
            {
                base.OnDataReceive(e);
            }
        }

        public async Task Write(string text)
        {
            DataFrame dataFrame = new DataFrame();
            dataFrame.Body = Encoding.UTF8.GetBytes(text);
            await Send(dataFrame);
        }

        private TaskCompletionSource<string> mReceiveCompletionSource;

        public async Task<string> SyncWrite(string text)
        {
            await Write(text);
            mReceiveCompletionSource = new TaskCompletionSource<string>();
            var result = await mReceiveCompletionSource.Task;
            return result;
        }
    }
}
