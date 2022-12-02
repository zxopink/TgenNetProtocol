using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TgenNetProtocol
{
    internal class StandardClientFactroy : IClientsFactory<ClientInfo>
    {
        private int _idCounter { get; set; }
        public StandardClientFactroy()
        {
            _idCounter = 0;
        }
        public ClientInfo PeerConnection(Socket sock)
        {
            return new ClientInfo(sock, _idCounter++);
        }
    }
}
