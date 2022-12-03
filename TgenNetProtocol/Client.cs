using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    internal class Client : IDisposable
    {
        private Socket client;
        public Socket Socket { get => client; }

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

        public bool Connected => IsActive;

        public bool IsActive { get { try { return client.Connected; } catch { return false; } } }

        public Client(Socket socket)
        {
            this.client = socket;
        }

        public void Connect(string ip, int port)
        {
            client.Connect(ip, port);
            client.NoDelay = true;
        }

        public void Close()
        {
            client.Close();
        }

        public void Dispose()
        {
            Close();
        }

        public static implicit operator bool(Client client) => client.IsActive;
        public static implicit operator NetworkStream(Client client) => client.NetworkStream;
        public static implicit operator Socket(Client client) => client.client;
        public static implicit operator EndPoint(Client client) => client.RemoteEndPoint;
        public static implicit operator IPEndPoint(Client client) => (IPEndPoint)client.RemoteEndPoint;

        public static explicit operator Client(Socket socket) => new Client(socket);
    }

}
