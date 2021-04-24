using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Text;

namespace Northwind.Data
{
    public class DataHelper
    {
        public DataHelper()
        {
            Assembly assembly = typeof(DataHelper).Assembly;

            using (System.IO.StreamReader reader =
                new System.IO.StreamReader(assembly.GetManifestResourceStream("Northwind.Data.Employees.txt")))
            {
                Employees = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Employee>>(reader.ReadToEnd());
            }


            using (System.IO.StreamReader reader =
               new System.IO.StreamReader(assembly.GetManifestResourceStream("Northwind.Data.Customers.txt")))
            {
                Customers = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Customer>>(reader.ReadToEnd());
            }


            using (System.IO.StreamReader reader =
              new System.IO.StreamReader(assembly.GetManifestResourceStream("Northwind.Data.Orders.txt")))
            {
                Orders = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Order>>(reader.ReadToEnd());
            }

            using (System.IO.StreamReader reader =
            new System.IO.StreamReader(assembly.GetManifestResourceStream("Northwind.Data.OrderBase.txt")))
            {
                OrderBases = Newtonsoft.Json.JsonConvert.DeserializeObject<List<OrderBase>>(reader.ReadToEnd());
            }

        }

        public List<OrderBase> OrderBases;

        public List<Employee> Employees;

        public List<Customer> Customers;

        public List<Order> Orders;

        private static DataHelper mDefault;

        public static DataHelper Defalut
        {
            get
            {
                if (mDefault == null)
                    mDefault = new DataHelper();
                return mDefault;
            }
        }

        public Employee GetEmployee(int id)
        {
            return Employees[id];
        }

        public Order GetOrder(int id)
        {
            return Orders[id];
        }

        public OrderBase GetOrderBase(int id)
        {
            return OrderBases[id];
        }

        public Customer GetCustomer(int id)
        {
            return Customers[id];
        }
    }
}
