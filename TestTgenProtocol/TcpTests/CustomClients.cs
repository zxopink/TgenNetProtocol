using NUnit.Framework.Constraints;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace TestTgenProtocol.TcpTests
{
    public class User : IPeerInfo
    {
        public string Name { get; set; }
        public NetworkStream NetworkStream { get; set; }

        public Socket Socket { get; set; }

        public IPEndPoint EndPoint => Socket.RemoteEndPoint as IPEndPoint;
        public User(Socket s)
        {
            Socket = s;
            NetworkStream = new NetworkStream(s);
        }
        public bool Equals(IPeerInfo clientData)
        {
            if (clientData is not User otherUser) return false;
            return Name == otherUser.Name;
        }
    }

    public class Fac : IClientsFactory<User>
    {
        int _counter = 0;
        string[] names = { "yoav", "tomer", "mihal","amir", "shmuel" }; 
        public User PeerConnection(Socket sock)
        {
            User u = new User(sock);
            _counter %= names.Length;
            u.Name = names[_counter++];
            return u;
        }
    }

    public class CustomClients : NetworkBehavour
    {
        ServerManager<User> Server;
        ClientManager Client1;
        ClientManager Client2;
        public CustomClients()
        {
            const int PORT = 1351;
            Server = new ServerManager<User>(PORT, new Fac());
            Client1 = new ClientManager();
            Client2 = new ClientManager();

            Server.Start();
            Server.ManagePollEvents(50);
            Client1.ManagePollEvents(50);
            Client2.ManagePollEvents(50);

            Client1.Connect("127.0.0.1", PORT);
            Client2.Connect("127.0.0.1", PORT);

            Client1.Send(("Hello!",1));
            Client2.Send(("Hello again!", 2));
        }

        bool cl1 = false;
        bool cl2 = false;
        [Test]
        public async Task CheckRecv()
        {
            Task timeout = Task.Delay(10000); //10 sec
            Task wait = WaitForPackets();

            Task winner = await Task.WhenAny(timeout, wait);
            if (winner == timeout)
                Assert.Fail("Didn't get custom user message");
        }

        public async Task WaitForPackets()
        {
            while (!cl1 && !cl2)
                await Task.Delay(50);
        }

        [ServerReceiver]
        public void ClientRecv((string text, int id) msg, User user)
        {
            if (msg.id == 1)
                cl1 = true;
            if (msg.id == 2)
                cl2 = true;
            Console.WriteLine("User " + user.Name + " sent: " + msg.text);
        }
    }
}
