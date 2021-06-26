# HttpClients
BeetleX http and websocket clients for .net standard2.0
## Install
```
Install-Package BeetleX.Http.Clients -Version 1.6
```
``` csharp
            var result = await "https://www.baidu.com/"
                    .FormUrlRequest()
                    .Get();
            Console.WriteLine(result.Body);

            result = await "https://httpbin.org/get"
                     .FormUrlRequest()
                     .Get();
            Console.WriteLine(result.Body);


            result = await "https://httpbin.org/post"
                     .JsonRequest()
                     .SetBody(DateTime.Now)
                     .Post();
            JToken rdata = result.GetResult<JToken>()["data"];

            Console.WriteLine(rdata);


            var buffer = await "https://httpbin.org/image"
                           .BinaryRequest()
                           .Download();

             result = await "http://localhost/Upload"
                           .FormDataRequest()
                           .Upload("g:\\extension_1_4_3_0.rar", "g:\\extension_1_4_3_0_1.rar");
```

### Http Cluster
``` csharp
HttpCluster httpCluster = new HttpCluster();
httpCluster.DefaultNode
.Add("http://192.168.2.25:8080")
.Add("http://192.168.2.26:8080");
var client = httpCluster.JsonRequest("/customers?count=10");
var data = await client.Get();
client = httpCluster.JsonRequest("/orders?size=10");
data = await client.Get();
```
### Http Cluster interface
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
``` csharp
HttpCluster httpClusterApi = new HttpClusterApi();
httpCluster.DefaultNode.Add("http://localhost:8080");
northWind = httpCluster.Create<INorthWind>();
var result = await northWind.GetEmployee(1);
```
### Multi server
``` csharp
httpCluster.DefaultNode
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
