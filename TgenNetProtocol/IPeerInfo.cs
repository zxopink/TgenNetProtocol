using System.Net;
using System.Net.Sockets;

namespace TgenNetProtocol
{
    public interface IPeerInfo
    {
        NetworkStream NetworkStream { get; } //Might be unsupported for UDP connections
        Socket Socket { get; }
        IPEndPoint EndPoint { get; }
        bool Equals(IPeerInfo clientData);
    }
}
