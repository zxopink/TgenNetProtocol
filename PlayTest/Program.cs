
using TgenNetProtocol;

//Start server
ServerManager server = new ServerManager(port: 4568).Start();
Task serverPollEvents = server.ManagePollEvents(millisecondsTimeOutPerPoll: 50);

//Recieve message server (Register a callback)
server.Register<string>((msg, client) => Console.WriteLine($"{client} sent {msg}"));
server.Unregister<string>(); //Remove all the registered callbacks to `string` type

//Wait for message server
string message = await server.WaitFor<string>(server.Clients[0], timeout: TimeSpan.FromSeconds(5));
//Start client
ClientManager client = new ClientManager();
client.Connect("127.0.0.1", 4568);
Task clientPollEvents = client.ManagePollEvents(millisecondsTimeOutPerPoll: 50);

//Recieve message client (Register a callback)
client.Register<string>(msg => Console.WriteLine($"server sent {msg}"));
client.Unregister<string>(); //Remove all the registered callbacks to `string` type

//Send Message Client
client.Send("Hello server!");

//Send Message Server
server.SendToAll("Hello clients!"); //Send all clients
server.Send("Hello client 1", server.Clients[index: 0]); //Send to specific client
server.SendToAllExcept("Hello everyone but client 1", server.Clients[index: 0]); //Send to everyone except a specific client

while (true)
{ 
}