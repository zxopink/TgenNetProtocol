using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace TestTgenProtocol.P2PTests
{
    public class Peer : NetworkBehavour
    {
        public int ID { get; set; }
        public UdpManager Manager { get; set; }

        public string messageRecv = null;
        public Peer(int id) : base(new UdpManager())
        {
            ID = id;
            Manager = (UdpManager)this.NetManagers[0];
            Manager.ManagePollEvents(50);
        }

        public async Task Setup()
        {
            var isOutGoing = (NetPeer peer) => peer.ConnectionState == ConnectionState.Outgoing;
            var isDisconnected = (NetPeer peer) => peer.ConnectionState == ConnectionState.Disconnected;
            var isConnected = (NetPeer peer) => peer.ConnectionState == ConnectionState.Connected;
            const int PORT = 7548;
            var server = Manager.Connect("127.0.0.1", PORT);
            while (isOutGoing(server))
                await Task.Delay(50);
            if (isDisconnected(server))
                Assert.Fail("Failed to connect to server");

            var partner = await Manager.RequestNatPunch();
            //NetPeer partner = null;
            //Manager.PeerConnectedEvent += (peer) => partner = peer;
            //Manager.RequestNatPunch();
            //var waitConnFunc = async () => { while (partner == null) await Task.Delay(50); };
            //var waiterConn = waitConnFunc();
            //var winnerConn = Task.WhenAny(waiterConn, Task.Delay(1500));
            //if(winnerConn != waiterConn)
            //    Assert.Fail("Failed to connect to peer");

            while (isOutGoing(partner))
                await Task.Delay(50);
            if (isDisconnected(partner))
                Assert.Fail("Failed to connect to peer");


            partner.Send($"Hello from {ID}");

            var waitFunc = async () => { while (messageRecv == null) await Task.Delay(50); };
            var waiter = waitFunc();
            var winner = Task.WhenAny(waiter, Task.Delay(1500));
            if (winner != waiter)
                Assert.Fail("Didn't get peer's message. " + (isConnected(partner) ? "Still connected" : "Peer disconnected"));
        }

        [DgramReceiver]
        public void GetHello(string hello)
        {
            messageRecv = hello;
        }

    }
}
