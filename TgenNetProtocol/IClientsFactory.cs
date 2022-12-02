using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public interface IClientsFactory<PeerType> 
        where PeerType : IPeerInfo
    {

        /// <summary>Called on newly accepted connection (After password check)</summary>
        /// <param name="sock">The new socket</param>
        /// <param name="netManager">Manger that accepted the incoming socket</param>
        /// <returns>A new PeerInfo</returns>
        PeerType PeerConnection(Socket sock);
    }
}
