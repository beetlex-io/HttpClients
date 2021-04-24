# HttpClients
BeetleX http and websocket clients for .net standard2.0
## Install
```
Install-Package BeetleX.Http.Clients -Version 1.5
```
``` csharp
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
```

## WebApi
### Defined interface
``` csharp
    public interface INorthWind
    {
        Task<Employee> GetEmployee(int id);
        [Post]
        Task<Employee> Add(Employee emp);
        [Post]
        Task<bool> Login(string name, string value);       
        [Post]
        Task<Employee> Modify([CQuery]int id, Employee body);
    }
```
### Create interface
``` csharp
HttpClusterApi httpClusterApi = new HttpClusterApi();
httpClusterApi.DefaultNode.Add("http://localhost:8080");
northWind = httpClusterApi.Create<INorthWind>();
var result = await northWind.GetEmployee(1);
```
### Multi server
``` csharp
httpClusterApi.DefaultNode
    .Add("http://192.168.2.25:8080")
    .Add("http://192.168.2.26:8080");
```
### Server weight
``` csharp
.Add("http://192.168.2.25:8080",10)
.Add("http://192.168.2.26:8080",10);
.Add("http://192.168.2.27:8080",5);
```
### Multi url route
```
httpClusterApi.GetUrlNode("/order.*")
    .Add("http://192.168.2.25:8080")
    .Add("http://192.168.2.26:8080");
httpClusterApi.GetUrlNode("/employee.*")
    .Add("http://192.168.2.27:8080")
    .Add("http://192.168.2.28:8080");
```
### github auth sample
``` csharp
    [FormUrlFormater]
    [Host("https://github.com")]
    public interface IGithubAuth
    {

        [Get(Route = "login/oauth/access_token")]
        Task<string> GetToken(string client_id, string client_secret, string code);

        [Host("https://api.github.com")]
        [CHeader("User-Agent", "beetlex.io")]
        [Get(Route = "user")]
        Task<string> GetUser(string access_token);
    }
     githubAuth = HttpApiClient.Create<IGithubAuth>();
```

### Websocket
### Create wsclient
```
TextClient client = new TextClient("ws://echo.websocket.org");
```
### send text
```
 await client.Send("hello");
```
### send and receive
```
var resutl = await wss.ReceiveFrom("hello henry");
```
