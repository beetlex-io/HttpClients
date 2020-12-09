using System;
using System.Collections.Generic;
using System.Text;

namespace BeetleX.Http.Clients
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PostAttribute : Attribute
    {
        public string Route { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class GetAttribute : Attribute
    {
        public string Route { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class DelAttribute : Attribute
    {
        public string Route { get; set; }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class PutAttribute : Attribute
    {
        public string Route { get; set; }
    }
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Parameter, AllowMultiple = true)]
    public class HeaderAttribute : Attribute
    {
        public HeaderAttribute()
        {

        }
        public HeaderAttribute(string name)
        {
            Name = name;
        }
        public HeaderAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }
        public string Name { get; set; }

        public string Value { get; set; }
    }
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Interface, AllowMultiple = true)]
    public class QueryAttribute : Attribute
    {
        public QueryAttribute()
        {

        }

        public QueryAttribute(string name)
        {
            Name = name;
        }
        public QueryAttribute(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; set; }

        public string Value { get; set; }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class RequestMaxRPS : Attribute
    {
        public RequestMaxRPS(int value)
        {
            Value = value;
        }

        public int Value { get; set; }
    }
}
