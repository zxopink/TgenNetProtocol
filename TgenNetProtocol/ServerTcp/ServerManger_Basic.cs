using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace TgenNetProtocol
{
    /// <summary>Basic server manager with ClientInfo for ClientType</summary>
    public class ServerManager : ServerManager<ClientInfo>
    {
        public ServerManager(int port) : base(port, new StandardClientFactroy())
        {
            //Empty
        }

        public ServerManager(IPAddress localaddr, int port) : base(localaddr, port, new StandardClientFactroy())
        {
            //Empty
        }

        public ServerManager(IPEndPoint localEP) : base(localEP, new StandardClientFactroy())
        {
            //Empty
        }

        public ServerManager(int port, IFormatter formatter) : base(port, formatter, new StandardClientFactroy())
        {
            //Empty
        }

        public ServerManager(IPAddress localaddr, int port, IFormatter formatter) : base(localaddr, port, formatter, new StandardClientFactroy())
        {
            //Empty
        }

        public ServerManager(IPEndPoint localEP, IFormatter formatter) : base(localEP, formatter, new StandardClientFactroy())
        {
            //Empty
        }

        public new ServerManager Start(int backlog) => (ServerManager)base.Start(backlog);
        public new ServerManager Start() => (ServerManager)base.Start();
    }
}
