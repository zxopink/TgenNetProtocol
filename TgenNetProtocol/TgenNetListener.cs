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
    public class TgenNetListener : INetEventListener, IDeliveryEventListener, INtpEventListener
    {
        public TgenNetListener()
        {
        }

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
            Console.WriteLine("Got message");
            ReceivedData(peer.EndPoint, reader.GetRemainingBytes());
        }

        public void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Console.WriteLine("Got unconnected message");
            ReceivedData(remoteEndPoint, reader.GetRemainingBytes());
        }

        private void ReceivedData(IPEndPoint endPoint, byte[] data)
        {
            try
            {
                UdpInfo info = new UdpInfo(endPoint);
                var obj = Bytes.ByteToClass(data);
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

        public void OnPeerConnected(NetPeer peer)
        {
            Console.WriteLine("Peer connected!");
        }

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            Console.WriteLine("Peer disconnected");
        }
    }
}
