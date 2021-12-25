using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public interface INetInfo
    {
        bool Equals(INetInfo clientData);
    }
}
