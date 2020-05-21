using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.Clients
{
    [AttributeUsage(AttributeTargets.Interface| AttributeTargets.Method| AttributeTargets.Class)]
    public class HostAttribute:Attribute
    {
        public HostAttribute(string name)
        {
            Host = new Uri(name);
        }

        public Uri Host { get; set; }
    }
}
