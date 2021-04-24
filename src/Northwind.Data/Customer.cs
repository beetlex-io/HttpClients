
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MessagePack;
using ProtoBuf;
namespace Northwind.Data
{
    [MessagePackObject]
    [ProtoContract]
    public class Customer
    {
        [ProtoMember(1)]
        [Key(0)]
        public string CustomerID { get; set; }
        [ProtoMember(2)]
        [Key(1)]
        public string CompanyName { get; set; }
        [ProtoMember(3)]
        [Key(2)]
        public string ContactName { get; set; }
        [ProtoMember(4)]
        [Key(3)]
        public string ContactTitle { get; set; }
        [ProtoMember(5)]
        [Key(4)]
        public string Address { get; set; }
        [ProtoMember(6)]
        [Key(5)]
        public string City { get; set; }
        [ProtoMember(7)]
        [Key(6)]
        public string PostalCode { get; set; }
        [ProtoMember(8)]
        [Key(7)]
        public string Country { get; set; }
        [ProtoMember(9)]
        [Key(8)]
        public string Phone { get; set; }
        [ProtoMember(10)]
        [Key(9)]
        public string Fax { get; set; }
    }
}
