using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public class Client
    {
        private TcpClient tcpClient;

        private bool controlled { get => controlledInstance != null; }
        public bool IsControlled { get => controlled; }

        public NetworkStream NetworkStream
        {
            get {
                if (!this) throw new Exception("Can't get NetworkStream, Client is not connected");
                return tcpClient.GetStream();
            } 
        }

        public bool IsActive { get { try { return tcpClient.Connected; } catch { return false; } } }

        public Client(TcpClient tcpClient)
        {
            this.tcpClient = tcpClient;
            controlledInstance = null;
        }

        public void Connect(string ip, int port)
        {
            tcpClient.Connect(ip, port);

            tcpClient.NoDelay = true; //disables delay which occures when sending small chunks or data
            tcpClient.Client.NoDelay = true; //disables delay which occures when sending small chunks or data
        }

        public void Close()
        {
            tcpClient.Close();
        }

        ControlledClient controlledInstance;
        public ControlledClient TakeControl()
        {
            if (controlledInstance == null)
            {
                controlledInstance = new ControlledClient(this);
                return controlledInstance;
            }
            else
                return controlledInstance;
        }

        public void ReturnControl()
        {
            if (controlledInstance != null)
            {
                controlledInstance.Dispose();
                controlledInstance = null;
            }
        }
        public void ReturnControl(ControlledClient controlInstance)
        {
            if (controlInstance == controlledInstance)
            {
                controlledInstance.Dispose();
                controlledInstance = null;
            }
            else
                throw new Exception("The given controlInstance does not fit the one taken");
        }

        public static implicit operator bool(Client client) => client.IsActive;
        public static implicit operator ControlledClient(Client client) => client.TakeControl();
        public static implicit operator NetworkStream(Client client) => client.NetworkStream;
        public static implicit operator TcpClient(Client client) => client.tcpClient;

        public static explicit operator Client(TcpClient tcpClient) => new Client(tcpClient);
    }

}
