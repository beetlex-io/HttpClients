# HttpClients
BeetleX http and websocket clients for .net standard2.0
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

### Websocket
### Create wsclient
```
TextClient client = new TextClient("ws://echo.websocket.org");

```
### receive event
```
client.DataReceive += (o, e)=>{
       Console.WriteLine(e.Message);
};
```
### send text
```
 await client.Write("hello");
```
### send and receive
```
var result = await client.SyncWrite("hello");
```
