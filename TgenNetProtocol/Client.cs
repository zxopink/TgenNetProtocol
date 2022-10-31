using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public class Client : IDisposable
    {
        private Socket client;
        public Socket Socket { get => client; }

        private bool controlled { get => controlledInstance != null; }
        public bool IsControlled { get => controlled; }

        public EndPoint RemoteEndPoint => client.RemoteEndPoint;
        public EndPoint LocalEndPoint => client.LocalEndPoint;

        private NetworkStream networkStream;
        public NetworkStream NetworkStream
        {
            get
            {
                if (!this) throw new SocketException((int)SocketError.NotConnected);
                if (networkStream != null)
                    return networkStream;

                return networkStream = new NetworkStream(client);
            }
        }

        public int Available => client.Available;

        public bool IsActive { get { try { return client.Connected; } catch { return false; } } }
        /*
        public Client(TcpClient tcpClient)
        {
            this.client = tcpClient.Client;
            controlledInstance = null;
        }
        */
        public Client(Socket socket)
        {
            this.client = socket;
            controlledInstance = null;
        }

        public void Connect(string ip, int port)
        {
            client.Connect(ip, port);

            client.NoDelay = true;
        }

        public Task ConnectAsync(string ip, int port)
        {
            return Task.Run(() => {
                client.Connect(ip, port);
                client.NoDelay = true; //disables delay which occures when sending small chunks or data
            });
        }

        public void Close()
        {
            client.Close();
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
                throw new Exception("The given controlInstance does not fit the taken one");
        }

        public void Dispose()
        {
            Close();
        }

        public static implicit operator bool(Client client) => client.IsActive;
        public static implicit operator ControlledClient(Client client) => client.TakeControl();
        public static implicit operator NetworkStream(Client client) => client.NetworkStream;
        public static implicit operator Socket(Client client) => client.client;
        public static implicit operator EndPoint(Client client) => client.RemoteEndPoint;
        public static implicit operator IPEndPoint(Client client) => (IPEndPoint)client.RemoteEndPoint;

        //Deprecated
        //public static explicit operator Client(TcpClient tcpClient) => new Client(tcpClient);
        public static explicit operator Client(Socket socket) => new Client(socket);
    }

}
