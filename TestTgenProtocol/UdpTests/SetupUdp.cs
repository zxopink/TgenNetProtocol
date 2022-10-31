using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace TestTgenProtocol.UdpTests
{
    [Serializable]
    public record TestUdpClass(int num, string name);
    internal class SetupUdp
    {
        ServerSideUdp ServerSide = new();
        ClientSideUdp ClientSide = new();
        public UdpManager server;
        public UdpManager client;

        public SetupUdp()
        {
            const int PORT = 8768;
            server = new UdpManager(PORT);
            server.Start();
            server.ManagePollEvents(50);
            client = new UdpManager();
            client.Start();
            client.Connect("127.0.0.1", PORT);
            client.ManagePollEvents(50);
            int count = 0;
            client.PeerConnectedEvent += (p) => count++;
            server.PeerConnectedEvent += (p) => count++;
            while (count < 2)
                Thread.Sleep(100);
        }

        [Test]
        public async Task TestClient()
        {
            int p = 5;
            string str = "Hello server";
            var m = new TestUdpClass(10, "Yoav client");

            ServerSide.Clear();

            client.SendToAll(p);
            client.SendToAll(str);
            client.SendToAll(m);

            await ServerSide.Received();
            Assert.That(p, Is.EqualTo(ServerSide.PrimitiveVal));
            Assert.That(str, Is.EqualTo(ServerSide.StringVal));
            Assert.That(m, Is.EqualTo(ServerSide.ManagedVal));
        }

        [Test]
        public async Task TestServer()
        {
            int p = 6;
            string str = "Hello client";
            var m = new TestUdpClass(11, "Yoav server");

            ClientSide.Clear();

            server.SendToAll(p);
            server.SendToAll(str);
            server.SendToAll(m);

            await ClientSide.Received();
            Assert.That(p, Is.EqualTo(ClientSide.PrimitiveVal));
            Assert.That(str, Is.EqualTo(ClientSide.StringVal));
            Assert.That(m, Is.EqualTo(ClientSide.ManagedVal));
        }

    }
}
