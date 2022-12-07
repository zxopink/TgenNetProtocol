using LiteNetLib;
using Microsoft.Win32.SafeHandles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TgenNetProtocol.Udp
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

        public void Deconstruct(out IPEndPoint local, out IPEndPoint remote)
        {
            local = Local;
            remote = Remote;
        }
    }
    public class NatMediator : INatPunchListener
    {
        /// <summary>Calls an event on NatIntroductionRequest before the waiting peer is added to the pending peers list</summary>
        public event Action<WaitPeer> OnRequest;
        public List<WaitPeer> PendingPeers { get; private set; }
        public NetManager Manager { get; private set; }
        public NatPunchModule Module => Manager.NatPunchModule;

        /// <summary>Only accept incoming endpoints from peers who are already connected to the manager. 
        /// true by default</summary>
        public bool OnlyAcceptConnectedPeers { get; set; } = true;
        public NatMediator(NetManager manager)
        {
            PendingPeers = new List<WaitPeer>();
            Manager = manager;
            Module.Init(this);
            Manager.NatPunchEnabled = true;
        }

        public void Pair(WaitPeer host, WaitPeer client) => Pair(host, client, string.Empty);
        public void Pair(WaitPeer host, WaitPeer client, string additionalInfo)
        {
            PendingPeers.Remove(host);
            PendingPeers.Remove(client);
            Introduce(host, client, additionalInfo);
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
                if (peer.EndPoint.Equals(remoteEndPoint))
                    found = peer;
            }
            if (found == default && OnlyAcceptConnectedPeers) return;

            var waitPeer = new WaitPeer(localEndPoint, remoteEndPoint, found, token);
            PendingPeers.Add(waitPeer);
            OnRequest?.Invoke(waitPeer);
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            //As server, we don't care
        }
    }
}
