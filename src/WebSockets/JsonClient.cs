using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
namespace BeetleX.Http.WebSockets
{
    public class JsonClient:WSClient
    {
        public JsonClient(string host) : base(host) { }

        public JsonClient(Uri host) : base(host) { }

        protected override void OnDataReceive(WSReceiveArgs e)
        {
            try
            {
                if(e.Frame !=null && e.Frame.Length>0)
                {
                    string text = Encoding.UTF8.GetString(e.Frame.Body, 0, e.Frame.Body.Length);
                    e.Message = Newtonsoft.Json.JsonConvert.DeserializeObject(text);
                }
            }
            catch(Exception e_)
            {
                e.Error = e_;
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
                        rec.TrySetResult((JToken)e.Message);
                    }
                });
            }
            else
            {
                base.OnDataReceive(e);
            }
        }

        public async Task Write(object data)
        {
            string text = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            DataFrame df = new DataFrame();
            df.Body = Encoding.UTF8.GetBytes(text);
            await Send(df);
        }

        private TaskCompletionSource<JToken> mReceiveCompletionSource;

        public async Task<JToken> SyncWrite(object data)
        {
            mReceiveCompletionSource = new TaskCompletionSource<JToken>();
            await Write(data);
            var result=await mReceiveCompletionSource.Task;
            return result;
        }

    }
}
