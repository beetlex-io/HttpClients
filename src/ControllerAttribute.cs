using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.Clients
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface)]
    public class ControllerAttribute : Attribute
    {
        public ControllerAttribute()
        {
            
        }
        public string BaseUrl { get; set; }
    }
}
