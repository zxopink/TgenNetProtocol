﻿using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace TgenNetProtocol
{
    /// <summary>
    /// A class made to keep track of clients for the serverManager
    /// </summary>
    public class ClientInfo : IPeerInfo
    {
        
        public IPEndPoint EndPoint { get => (IPEndPoint)Client.RemoteEndPoint; }
        public bool Connected { get => Client.IsActive; }
        internal Client Client { get; private set; }
        public int Id { get; private set; }

        public NetworkStream NetworkStream => Client.NetworkStream;
        public Socket Socket => Client.Socket;

        internal ClientInfo(Client client, int id)
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
        public bool Equals(IPeerInfo clientData) =>
            Equals((object)clientData);

        public static implicit operator int(ClientInfo clientData) => clientData.Id;
        public static implicit operator bool(ClientInfo clientData) => clientData.Client;
        public static implicit operator NetworkStream(ClientInfo client) => client.Client;
        public static implicit operator Socket(ClientInfo client) => client.Client;

        public override string ToString() => Id.ToString();

        public override int GetHashCode() => base.GetHashCode();

        public static bool operator ==(ClientInfo a, ClientInfo b)
        => a.Equals(b);

        public static bool operator !=(ClientInfo a, ClientInfo b)
        => !a.Equals(b);
    }
}
