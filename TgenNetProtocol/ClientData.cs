using System.Net.Sockets;

namespace TgenNetProtocol
{
    /// <summary>
    /// A struct made to keep track of clients for the serverManager
    /// </summary>
    public struct ClientData
    {
        public Client client;
        public int id;

        public ClientData(Client client, int id)
        {
            this.client = client;
            this.id = id;
        }
        public ClientData(TcpClient tcpClient, int id)
        {
            client = (Client)tcpClient;
            this.id = id;
        }

        public static implicit operator bool(ClientData clientData) => clientData.client;
        public static implicit operator int(ClientData clientData) => clientData.id;
        public static implicit operator NetworkStream(ClientData client) => client.client;
        public static implicit operator TcpClient(ClientData client) => client.client;
        public override string ToString() => id.ToString();
    }
}
