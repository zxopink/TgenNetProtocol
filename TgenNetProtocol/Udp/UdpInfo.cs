using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    internal struct UdpInfo : IPeerInfo
    {
        public IPEndPoint EndPoint { get; private set; }
        public NetPeer Peer { get; set; }

        public NetworkStream NetworkStream => throw new NotSupportedException($"A udp peer, doesn't have {nameof(NetworkStream)}");

        public Socket Socket => throw new NotSupportedException($"A udp peer, doesn't have {nameof(Socket)}");

        public UdpInfo(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
            Peer = null;
        }
        public UdpInfo(NetPeer peer)
        {
            this.Peer = peer;
            this.EndPoint = peer.EndPoint;
        }
        public override bool Equals(object obj) =>
            obj is UdpInfo ? Equals((UdpInfo)obj) : false ;

        public bool Equals(IPeerInfo clientData)
        {
            UdpInfo data = (UdpInfo)clientData;
            return EndPoint.Equals(data.EndPoint);
        }

        public static bool operator ==(UdpInfo a, UdpInfo b)
        => a.Equals(b);

        public static bool operator !=(UdpInfo a, UdpInfo b)
        => !a.Equals(b);
    }
}
