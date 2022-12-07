using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Text;
using TgenSerializer;

namespace TgenNetProtocol.Udp
{
    public static class TgenPeer
    {
        public static void Send(this NetPeer peer, object obj, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            byte[] objGraph = new TgenFormatter().Serialize(obj);
            peer.Send(objGraph, deliveryMethod);
        }
    }
}
