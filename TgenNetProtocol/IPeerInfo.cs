using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

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
