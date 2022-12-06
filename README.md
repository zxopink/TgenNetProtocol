# TgenNetProtocol
An easy to work with network protocol!
also works with winforms!
you can use both client and server in the same project, but it is recommended to separate them and use shared types in a third project.

*if you send a custom type make sure it is serializable by using the [serializable] attribute!

server side
--------------------------------------------------------------------------------------------------------------------------------

`Starting server`:
```cs
ServerManager server = new ServerManager(port: 4568).Start();
Task serverPollEvents = server.ManagePollEvents(millisecondsTimeOutPerPoll: 50);
```

`Send message`:
```cs
server.SendToAll("Hello clients!"); //Send all clients
server.Send("Hello client 1", server.Clients[index: 0]); //Send to specific client
server.SendToAllExcept("Hello everyone but client 1", server.Clients[index: 0]); //Send to everyone except a specific client
```

`Receive a message (Register callbacks)`:
```cs
server.Register<string>((msg, client) => Console.WriteLine($"{client} sent {msg}"));

/*to ungregister methods:*/
server.Unregister<string>(); //Remove all the registered callbacks to `string` type
```

`Receive a message (Await packet)`:
```cs
string message = await server.WaitFor<string>(server.Clients[0]);
//Or use a timeout (will return default if timed-out)
string message = await server.WaitFor<string>(server.Clients[0], timeout: TimeSpan.FromSeconds(5));
```

client side
--------------------------------------------------------------------------------------------------------------------------------

`starting a server`:
for the client side there's a ClientManager class which manages the client side listener.
to start the client all you need to do is create a new object of ClientManager type with the port and ip you'd like the client to connect the server, then when use the method "connect" to connect the server.

`send a message`:
you can send a message to the server by using the "Send" method.

`receive a message`:
this one is fairly easy too and similar to the server.
just like the server you need to make a new method inside your class (make sure the class inherits from the "NetworkBehavour" class or "FormNetworkBehavour" if you work with forms) and put a "[ClientReceiver]" attributes on it!
the method must return void and take one custom argument (whatever type you choose).
the method will be invoked whenever the type of it's first argument was recived by the server, if the first argument of the method is type of object the method will be called everytime a message is Recived.

notes
--------------------------------------------------------------------------------------------------------------------------------
please credit me if you use this protocol!
and please leave a review or report any bugs! it's my first github project and I want to improve so I can make more projects like this!

made by: Yoav Haik
