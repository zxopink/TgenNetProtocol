using System.Net;
using System.Net.Sockets;

namespace TgenNetProtocol
{
    /// <summary>
    /// A struct made to keep track of clients for the serverManager
    /// </summary>
    public struct ClientInfo : INetInfo
    {
        public IPEndPoint EndPoint { get => (IPEndPoint)client.RemoteEndPoint; }
        public Client client;
        public int id;

        public ClientInfo(Client client, int id)
        {
            this.client = client;
            this.id = id;
        }
        public ClientInfo(Socket socket, int id)
        {
            client = (Client)socket;
            this.id = id;
        }

        public override bool Equals(object other)
        {
            return other is ClientInfo otherInfo ? otherInfo.id == id : false;
        }
        public bool Equals(INetInfo clientData)
        {
            return Equals(clientData);
        }

        public static implicit operator int(ClientInfo clientData) => clientData.id;
        public static implicit operator bool(ClientInfo clientData) => clientData.client;
        public static implicit operator NetworkStream(ClientInfo client) => client.client;
        public static implicit operator Socket(ClientInfo client) => client.client;

        public override string ToString() => id.ToString();

        public static bool operator ==(ClientInfo a, ClientInfo b)
        => a.Equals(b);

        public static bool operator !=(ClientInfo a, ClientInfo b)
        => !a.Equals(b);
    }
}
