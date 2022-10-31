using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Text;
using TgenSerializer;
using Formatter = TgenSerializer.Formatter;

namespace TgenNetProtocol
{
    public partial class UdpManager : INetEventListener, IDeliveryEventListener, INtpEventListener, IDisposable
    {
        public void OnConnectionRequest(ConnectionRequest request)
        {
            request.AcceptIfKey(ConnectionKey);
        }

        public void OnMessageDelivered(NetPeer peer, object userData)
        {
            Console.WriteLine("Received message/userData?: " + userData);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) =>
            NetworkErrorEvent?.Invoke(endPoint, socketError);

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) =>
            NetworkLatencyUpdateEvent?.Invoke(peer, latency);

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod deliveryMethod)
        {
            ReceivedData(peer, peer.EndPoint, reader.GetRemainingBytes());
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            ReceivedData(null, remoteEndPoint, reader.GetRemainingBytes());
        }

        private void ReceivedData(NetPeer peer, IPEndPoint endPoint, byte[] data)
        {
            try
            {
                UdpInfo info = peer != null ? new UdpInfo(peer) : new UdpInfo(endPoint);
                var obj = Formatter.FromBytes(data);
                TypeSetter.SendNewDatagramMessage(obj, info);
            }
            catch (SerializationException e)
            {
                Console.WriteLine("Formatting exception");
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
        }

        public void OnNtpResponse(NtpPacket packet)
        {

        }

        public void OnPeerConnected(NetPeer peer) =>
            PeerConnectedEvent?.Invoke(peer);

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) =>
            PeerDisconnectedEvent?.Invoke(peer, disconnectInfo);
    }
}
