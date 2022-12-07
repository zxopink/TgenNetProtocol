# TgenNetProtocol
[![NuGet Package][NuGet]][NuGet-url]

An easy to work with network protocol!
also works with winforms!
you can use both client and server in the same project, but it is recommended to separate them and use shared types in a third project.

Server Side
--------------------------------------------------------------------------------------------------------------------------------

Start the server:
```cs
ServerManager server = new ServerManager(port: 4568).Start();
Task serverPollEvents = server.ManagePollEvents(millisecondsInterval: 50);
```

Send a message:
```cs
server.SendToAll("Hello clients!"); //Send all clients
server.Send("Hello client 1", server.Clients[index: 0]); //Send to specific client
server.SendToAllExcept("Hello everyone but client 1", server.Clients[index: 0]); //Send to everyone except a specific client
```

Receive a message (Register callbacks):
```cs
server.Register<string>((msg, client) => Console.WriteLine($"{client} sent {msg}"));

/*to ungregister methods:*/
server.Unregister<string>(); //Remove all the registered callbacks to `string` type
```

Receive a message (Await packet):
```cs
string message = await server.WaitFor<string>(server.Clients[0]);
//Or use a timeout (will return default if timed-out)
string message = await server.WaitFor<string>(server.Clients[0], TimeSpan.FromSeconds(5));
```

Client Side
--------------------------------------------------------------------------------------------------------------------------------

Start the client:
```cs
ClientManager client = new ClientManager();
client.Connect("127.0.0.1", 4568);
Task clientPollEvents = client.ManagePollEvents(millisecondsInterval: 50);
```

Send a message:
```cs
client.Send("Hello server!");
```

Receive a message (Register callbacks):
```cs
client.Register<string>(msg => Console.WriteLine($"server sent {msg}"));

/*to ungregister methods:*/
client.Unregister<string>(); //Remove all the registered callbacks to `string` type
```

Receive a message (Await packet):
```cs
string message = await client.WaitFor<string>();
//Or use a timeout (will return default if timed-out)
string message = await client.WaitFor<string>(TimeSpan.FromSeconds(5));
```

Notes
--------------------------------------------------------------------------------------------------------------------------------
*In this example I used strings but you can use any type that's primitive or has the `[Serializable]` attribute

Credit me if you use this protocol!
Made by: Yoav Haik

[NuGet]: https://img.shields.io/nuget/v/TgenNetProtocol?color=blue
[NuGet-url]: https://www.nuget.org/packages/TgenNetProtocol
