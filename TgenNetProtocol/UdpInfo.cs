﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public struct UdpInfo : INetInfo
    {
        public IPEndPoint EndPoint { get; set; }
        public UdpInfo(IPEndPoint endPoint)
        {
            EndPoint = endPoint;
        }
        public bool Equals(INetInfo clientData)
        {
            UdpInfo data = (UdpInfo)clientData;
            return EndPoint.Equals(data.EndPoint);
        }

        public static bool operator ==(UdpInfo a, UdpInfo b)
        => a.Equals(b);

        public static bool operator !=(UdpInfo a, UdpInfo b)
        => !a.Equals(b);
    }
}
