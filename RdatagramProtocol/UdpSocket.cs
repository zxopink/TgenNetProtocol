using System;
using System.Net.Sockets;

namespace RdatagramProtocol
{
    internal class UdpSocket
    {
        public Socket Socket { get; private set; }

        /// <param name="socket">A socket of protocol type 'Udp'</param>
        internal UdpSocket(Socket socket)
        {
            if (socket.ProtocolType != ProtocolType.Udp)
                throw new SocketException((int)SocketError.ProtocolType); //Socket must be of protocol type Udp
        }
    }
}
