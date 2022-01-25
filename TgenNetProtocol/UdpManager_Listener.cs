﻿using LiteNetLib;
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

        public void OnNetworkError(IPEndPoint endPoint, SocketError socketError) =>
            NetworkErrorEvent?.Invoke(endPoint, socketError);

        public void OnNetworkLatencyUpdate(NetPeer peer, int latency) =>
            NetworkLatencyUpdateEvent?.Invoke(peer, latency);

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
            PeerConnectedEvent?.Invoke(peer);

        public void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) =>
            PeerDisconnectedEvent?.Invoke(peer, disconnectInfo);
    }
}