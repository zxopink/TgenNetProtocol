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
            
            var peer = new WaitPeer(localEndPoint, remoteEndPoint, token);
            OnRequest?.Invoke(peer);
            PendingPeers.Add(peer);
        }

        public void OnNatIntroductionSuccess(IPEndPoint targetEndPoint, NatAddressType type, string token)
        {
            //As server, we don't care
        }
    }
}
