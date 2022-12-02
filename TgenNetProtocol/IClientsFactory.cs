using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace TgenNetProtocol
{
    public interface IClientsFactory<PeerType>
    {
        /// <summary>Called on newly accepted connection (After password check)</summary>
        /// <returns>A new PeerInfo</returns>
        PeerType PeerConnection(Socket sock);
    }
}
