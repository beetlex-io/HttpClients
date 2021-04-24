
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
    public class Employee
    {
        [ProtoMember(1)]
        [Key(0)]
        public int EmployeeID
        {
            get;
            set;
        }
        [ProtoMember(2)]
        [Key(1)]
        public string LastName
        {
            get;
            set;
        }
        [ProtoMember(3)]
        [Key(2)]
        public string FirstName
        {
            get;
            set;
        }
        [ProtoMember(4)]
        [Key(3)]
        public string Title
        {
            get;
            set;
        }
        [ProtoMember(5)]
        [Key(4)]
        public string TitleOfCourtesy { get; set; }
        [ProtoMember(6)]
        [Key(5)]
        public DateTime BirthDate { get; set; }
        [ProtoMember(7)]
        [Key(6)]
        public DateTime HireDate { get; set; }
        [ProtoMember(8)]
        [Key(7)]
        public string Address { get; set; }
        [ProtoMember(9)]
        [Key(8)]
        public string City { get; set; }
        [ProtoMember(10)]
        [Key(9)]
        public string Region { get; set; }
        [ProtoMember(11)]
        [Key(10)]
        public string PostalCode { get; set; }
        [ProtoMember(12)]
        [Key(11)]
        public string Country { get; set; }
        [ProtoMember(13)]
        [Key(12)]
        public string HomePhone { get; set; }
        [ProtoMember(14)]
        [Key(13)]
        public string Extension { get; set; }
        [ProtoMember(15)]
        [Key(14)]
        public string Photo { get; set; }
        [ProtoMember(16)]
        [Key(15)]
        public string Notes { get; set; }
    }
}
