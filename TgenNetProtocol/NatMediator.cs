using LiteNetLib;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace TgenNetProtocol
{
    public class WaitPeer
    {
        public IPEndPoint Local { get; private set; }
        public IPEndPoint Remote { get; private set; }
        public DateTime At { get; private set; }
        public string Token { get; private set; }

        /// <summary>Could be null depending on the NatMediator OnlyAcceptConnectedPeers field</summary>
        public NetPeer NetPeer { get; private set; } = default;
        public WaitPeer(IPEndPoint local, IPEndPoint remote, NetPeer peerInstance, string token) 
            : this(local, remote, token)
        {
            NetPeer = peerInstance;
        }
        public WaitPeer(IPEndPoint local, IPEndPoint remote, string token)
        {
            Token = token;
            Local = local;
            Remote = remote;
            At = DateTime.Now;
        }
    }
    internal class NatMediator : INatPunchListener
    {
        public event Action<WaitPeer> OnRequest;

        public List<WaitPeer> PendingPeers { get; private set; }
        public NetManager Manager { get; private set; }
        public NatPunchModule Module => Manager.NatPunchModule;
        public bool OnlyAcceptConnectedPeers { get; set; } = true;
        public NatMediator(NetManager manager)
        {
            PendingPeers = new List<WaitPeer>();
            Manager = manager;
            Manager.NatPunchEnabled = true;
            Module.Init(this);
        }

        public void Introduce(WaitPeer host, WaitPeer client, string additionalInfo)
        {
            Module.NatIntroduce(
                host.Local,
                host.Remote,
                client.Local,
                client.Remote,
                additionalInfo);
        }

        public void OnNatIntroductionRequest(IPEndPoint localEndPoint, IPEndPoint remoteEndPoint, string token)
        {
            NetPeer found = default;
            foreach (var peer in Manager.ConnectedPeerList)
            {
                if (peer.EndPoint == remoteEndPoint)
                    found = peer;
            }
            if (found == default && OnlyAcceptConnectedPeers) return;

            var waitPeer = new WaitPeer(localEndPoint, remoteEndPoint, found, token);
            OnRequest?.Invoke(waitPeer);
            PendingPeers.Add(waitPeer);
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            //As server, we don't care
        }
    }
}
