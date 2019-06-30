using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.Clients
{
    public interface IHeaderHandler
    {
       Dictionary<string,string> Header { get; }
    }
}
