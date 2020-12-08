using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public abstract class GeneralNetworkManager
    {
        public void Send(object message)
        {
            if (Equals(this, typeof(ClientManager)))
            {
                ClientManager client = (ClientManager)this;
                client.Send(message);
                return;
            }
            if (Equals(this, typeof(ServerManager)))
            {
                ServerManager server = (ServerManager)this;
                server.SendToAll(message);
            }
            throw new Exception("The object isn't a client or a server");
        }

        public virtual void Close()
        {
            if (Equals(this, typeof(ClientManager)))
            {
                ClientManager client = (ClientManager)this;
                client.Close();
                return;
            }
            if (Equals(this, typeof(ServerManager)))
            {
                ServerManager server = (ServerManager)this;
                server.Close();
            }
            throw new Exception("The object isn't a client or a server");
        }
    }
}
