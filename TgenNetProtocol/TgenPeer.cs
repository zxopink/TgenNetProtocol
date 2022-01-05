using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Text;
using TgenSerializer;

namespace TgenNetProtocol
{
    public static class TgenPeer
    {
        public static void Send(this NetPeer peer, object obj, DeliveryMethod deliveryMethod)
        {
            byte[] objGraph = Bytes.ClassToByte(obj);
            peer.Send(objGraph, deliveryMethod);
        }
        public static void Send(this NetPeer peer, ISerializable obj, DeliveryMethod deliveryMethod)
        {
            //Makes a DataWriter with the object's type
            var writer = new DataWriter(obj);
            obj.Serialize(writer);
            peer.Send(writer.GetData(), deliveryMethod);
        }
        public static void Send(this NetPeer peer, DataWriter obj, DeliveryMethod deliveryMethod) =>
            peer.Send(obj.GetData(), deliveryMethod);
    }
}
