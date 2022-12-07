using System.Net.Sockets;

namespace TgenNetProtocol
{
    public interface IClientsFactory<PeerType> 
        where PeerType : IPeerInfo
    {

        /// <summary>Called on newly accepted connection (After password check)</summary>
        /// <param name="sock">The new socket</param>
        /// <param name="netManager">The manager that accepted the incoming socket</param>
        /// <returns>A new PeerInfo</returns>
        PeerType PeerConnection(Socket sock, INetManager netManager);
    }
}
