using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.WebSockets
{
    public class WSReceiveArgs:System.EventArgs
    {
        public WSClient Client { get; internal set; }

        public DataFrame Frame { get; internal set; }

        public object Message { get; internal set; }

        public Exception Error { get; internal set; }
    }
}
