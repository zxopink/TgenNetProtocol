using LiteNetLib;
using LiteNetLib.Utils;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using TgenSerializer;

namespace TgenNetProtocol
{
    public partial class UdpManager
    {
        public void OnConnectionRequest(ConnectionRequest request)
        {
            Console.WriteLine("Connection request pending but accepting :)");
            request.Accept();
        }

        public void OnMessageDelivered(NetPeer peer, object userData)
        {
            Console.WriteLine("Received message/userData?: " + userData);
        }

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError)
        {
            Console.WriteLine($"Network error: {socketError}, with: {endPoint}");
        }

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency)
        {

        }

        public void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channelNumber, DeliveryMethod deliveryMethod)
        {
            ReceivedData(peer.EndPoint, reader.GetRemainingBytes());
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            ReceivedData(remoteEndPoint, reader.GetRemainingBytes());
        }

        private void ReceivedData(IPEndPoint endPoint, byte[] data)
        {
            try
            {
                UdpInfo info = new UdpInfo(endPoint);
                var obj = Bytes.ByteToClass(data);
                Console.WriteLine("got type on: " + obj.GetType());
                TypeSetter.SendNewDatagramMessage(obj, info);
            }
            catch (Exception e)
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
            ConnectedEvent?.Invoke(peer);

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) =>
            DisconnectedEvent?.Invoke(peer, disconnectInfo);
    }
}
