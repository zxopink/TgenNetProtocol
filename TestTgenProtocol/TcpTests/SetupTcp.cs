using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace TestTgenProtocol.TcpTests
{
    [Serializable]
    public record TestTcpClass(int num, string name);
    internal class SetupTcp
    {
        ServerSideTcp ServerSide = new();
        ClientSideTcp ClientSide = new();
        public ServerManager server;
        public ClientManager client;

        public SetupTcp()
        {
            const int PORT = 8768;
            server = new ServerManager(PORT);
            server.Start();
            server.ManagePollEvents(50);
            client = new ClientManager();
            client.Connect("127.0.0.1", PORT, true);
            client.ManagePollEvents(50);
        }

        [Test]
        public async Task TestClient()
        {
            int p = 5;
            string str = "Hello server";
            var m = new TestTcpClass(10, "Yoav client");

            client.Send(p);
            client.Send(str);
            client.Send(m);

            await ServerSide.Received();
            Assert.True(ServerSide.PrimitiveVal == p);
            Assert.True(ServerSide.StringVal == str);
            Assert.That(m, Is.EqualTo(ServerSide.ManagedVal));
        }

        [Test]
        public async Task TestServer()
        {
            int p = 6;
            string str = "Hello client";
            var m = new TestTcpClass(11, "Yoav server");

            server.SendToAll(p);
            server.SendToAll(str);
            server.SendToAll(m);

            await ClientSide.Received();
            Assert.True(ClientSide.PrimitiveVal == p);
            Assert.True(ClientSide.StringVal == str);
            Assert.That(m, Is.EqualTo(ClientSide.ManagedVal));
        }

    }
}
