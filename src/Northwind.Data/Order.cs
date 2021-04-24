
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
    public class Order
    {
        [ProtoMember(1)]
        [Key(0)]
        public int OrderID { get; set; }
        [ProtoMember(2)]
        [Key(1)]
        public string CustomerID { get; set; }
        [ProtoMember(3)]
        [Key(2)]
        public int EmployeeID { get; set; }
        [ProtoMember(4)]
        [Key(3)]
        public DateTime OrderDate { get; set; }
        [ProtoMember(5)]
        [Key(4)]
        public DateTime RequiredDate { get; set; }
        [ProtoMember(6)]
        [Key(5)]
        public DateTime ShippedDate { get; set; }
        [ProtoMember(7)]
        [Key(6)]
        public int ShipVia { get; set; }
        [ProtoMember(8)]
        [Key(7)]
        public double Freight { get; set; }
        [ProtoMember(9)]
        [Key(8)]
        public string ShipName { get; set; }
        [ProtoMember(10)]
        [Key(9)]
        public string ShipAddress { get; set; }
        [ProtoMember(11)]
        [Key(10)]
        public string ShipCity { get; set; }
        [ProtoMember(12)]
        [Key(11)]
        public string ShipPostalCode { get; set; }
        [ProtoMember(13)]
        [Key(12)]
        public string ShipCountry { get; set; }
    }
    [MessagePackObject]
    [ProtoContract]
    public class OrderBase
    {
        [ProtoMember(1)]
        [Key(0)]
        public int OrderID { get; set; }
        [ProtoMember(2)]
        [Key(1)]
        public int EmployeeID { get; set; }
        [ProtoMember(3)]
        [Key(2)]
        public DateTime OrderDate { get; set; }
        [ProtoMember(4)]
        [Key(3)]
        public DateTime RequiredDate { get; set; }
        [ProtoMember(5)]
        [Key(4)]
        public DateTime ShippedDate { get; set; }
    }
}
