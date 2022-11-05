using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace TestTgenProtocol.P2PTests
{
    [TestFixture]
    public class Puncher
    {
        UdpManager Manager { get; set; }
        public Puncher()
        {

        }

        [OneTimeSetUp]
        public async Task Setup()
        {
            const int PORT = 7548;
            Manager = new(PORT);
            Manager.Start();
            Manager.ManagePollEvents(50);
            var mediator = Manager.NatMediator;
            mediator.OnlyAcceptConnectedPeers = false;
        }

        //[Test]
        public async Task TestPeers()
        {
            int counter = 0;
            var mediator = Manager.NatMediator;
            mediator.OnRequest += (waitPeer) => counter++;
            Peer p1 = CreatPeer(1);
            Peer p2 = CreatPeer(2);
            var pTasks = new[] { p1.Setup(), p2.Setup() };
            while (counter < 2)
                await Task.Delay(50);
            mediator.Pair((peers) => (peers[0], peers[1]));
            await Task.WhenAll(pTasks);
        }


        public Peer CreatPeer(int id)
        {
            return new Peer(id);
        }

    }
}
