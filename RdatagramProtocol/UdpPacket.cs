using System;
using System.Collections.Generic;
using System.Text;
using TgenSerializer;

namespace RdatagramProtocol
{
    public interface UdpPacket<T>
    {
        void Serialize(DataWriter writer);

        T Deserialize(DataReader reader);
    }
}
