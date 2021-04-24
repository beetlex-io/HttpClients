using System;
using System.Threading.Tasks;
using Xunit;
using System.Collections.Generic;
using Northwind.Data;
using Newtonsoft.Json.Linq;

namespace BeetleX.Http.Clients.UnitTest
{
    public class HttpClientTest
    {



        [Fact]
        public async Task HttpBin_Delete()
        {
            HttpJsonClient client = new HttpJsonClient("http://httpbin.org");
            var result = await client.Delete("/delete");
            Assert.Equal(null, result.Exception);

        }
        [Fact]
        public async Task HttpBin_Get()
        {
            HttpJsonClient client = new HttpJsonClient("http://httpbin.org");
            var result = await client.Get("/get");
            Assert.Equal(null, result.Exception);
        }

        [Fact]
        public async Task HttpBin_Post()
        {
            HttpJsonClient client = new HttpJsonClient("http://httpbin.org");
            var date = DateTime.Now;
            client.SetBody(date);
            var result = await client.Post("/post");
            JToken rdata = result.GetResult<JToken>()["data"];
            Assert.Equal(date, rdata.ToObject<DateTime>());
        }
        [Fact]
        public async Task HttpBin_Put()
        {
            HttpJsonClient client = new HttpJsonClient("http://httpbin.org");
            Employee emp = DataHelper.Defalut.Employees[0];
            client.SetBody(emp);
            var result = await client.Post("/post");
            JToken rdata = result.GetResult<JToken>()["data"];
            Assert.Equal(emp.EmployeeID, rdata.ToObject<Employee>().EmployeeID);
        }
        [Fact]
        public async Task GetImage()
        {
            HttpClient<BinaryFormater> client = new HttpClient<BinaryFormater>("http://httpbin.org");
            var result = await client.Get("/image");
            var data = result.GetResult<ArraySegment<byte>>();
            using (System.IO.Stream write = System.IO.File.Create("test.jpg"))
            {
                write.Write(data.Array, data.Offset, data.Count);
                write.Flush();
            }
        }

    }
}
