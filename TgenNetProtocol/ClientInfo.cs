using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace TgenNetProtocol
{
    /// <summary>
    /// A struct made to keep track of clients for the serverManager
    /// </summary>
    public class ClientInfo : INetInfo
    {
        public IPEndPoint EndPoint { get => (IPEndPoint)Client.RemoteEndPoint; }
        public bool Connected { get => Client.IsActive; }
        public Client Client { get; private set; }
        public int Id { get; private set; }

        public ClientInfo(Client client, int id)
        {
            Client = client;
            Id = id;
        }
        public ClientInfo(Socket socket, int id)
        {
            Client = (Client)socket;
            Id = id;
        }

        public override bool Equals(object other)
        {
            return other is ClientInfo otherInfo ? otherInfo.Id == Id : false;
        }
        public bool Equals(INetInfo clientData) =>
            Equals((object)clientData);

        public static implicit operator int(ClientInfo clientData) => clientData.Id;
        public static implicit operator bool(ClientInfo clientData) => clientData.Client;
        public static implicit operator NetworkStream(ClientInfo client) => client.Client;
        public static implicit operator Socket(ClientInfo client) => client.Client;

        public override string ToString() => Id.ToString();

        public override int GetHashCode()
        {
            int hashCode = 1269177722;
            hashCode = hashCode * -1521134295 + EqualityComparer<IPEndPoint>.Default.GetHashCode(EndPoint);
            hashCode = hashCode * -1521134295 + Connected.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<Client>.Default.GetHashCode(Client);
            hashCode = hashCode * -1521134295 + Id.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(ClientInfo a, ClientInfo b)
        => a.Equals(b);

        public static bool operator !=(ClientInfo a, ClientInfo b)
        => !a.Equals(b);
    }
}
