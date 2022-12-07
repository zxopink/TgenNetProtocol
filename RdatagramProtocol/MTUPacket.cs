using System;
using System.Collections.Generic;
using System.Text;
using TgenSerializer;

namespace RdatagramProtocol
{
    /// <summary>maximum transition unit packet, a packet that's over the MTU limit</summary>
    public abstract class MTUPacket<T> : UdpPacket<T>
    {
        /// <summary>Max packet size</summary>
        public const int MaxSize = 1400;
        int segment;

        public MTUPacket()
        {
            
        }

        public abstract T Deserialize(DataReader reader);

        public abstract void Serialize(DataWriter writer);

        private void OnSerialize(DataWriter writer)
        {
            if (writer.data.Length > MaxSize)
            {
                
            }
        }
    }
}
