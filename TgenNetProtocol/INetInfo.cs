using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public interface INetInfo
    {
        IPEndPoint EndPoint { get; }
        bool Equals(INetInfo clientData);
    }
}
