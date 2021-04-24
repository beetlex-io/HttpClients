using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.Clients
{
    public class UploadFile
    {
        public string Name { get; set; }

        public ArraySegment<byte> Data { get; set; }
    }
}
