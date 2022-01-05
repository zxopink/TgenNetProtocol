using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TgenSerializer;
using System.Collections.Concurrent;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TgenNetProtocol
{
    public partial class UdpManager : INetEventListener, IDeliveryEventListener, INtpEventListener, IDisposable
    {
        public bool IsRunning => RUdpClient.IsRunning;
        public IPEndPoint LocalEP => _localEP;
        /// <summary>Local Port</summary>
        public int Port => _localEP.Port;
        //Used to set a listening port
        private IPEndPoint _localEP { get; set; }

        public bool NatPunchEnabled { get => RUdpClient.NatPunchEnabled; set => RUdpClient.NatPunchEnabled = value; }
        public NatPunchModule NatPunchModule { get => RUdpClient.NatPunchModule; }

        //Received data but failed to Deserialize it(Turn it into runtime object)
        public event UdpEvent PacketLoss;
        public delegate void UdpEvent(UdpInfo info, byte[] data);

        public NetManager RUdpClient { get; private set; }

        //Default connection key
        private const string CONN_KEY = "TgenKey";
        private string userKey = null;
        //If changed from null, will deny connections if keys don't match between peers
        public string ConnectionKey { get => userKey ?? CONN_KEY; set => userKey = value; }

        public delegate void DisconnectedFunc(NetPeer peer, DisconnectInfo disconnectInfo);
        public event DisconnectedFunc DisconnectedEvent;

        public delegate void PendingConnectionFunc(ConnectionRequest request);
        public event PendingConnectionFunc PendingConnectionEvent;

        public delegate void ConnectedFunc(NetPeer peer);
        ///<summary>Fires when a pending connection was accepted</summary>
        public event ConnectedFunc ConnectedEvent;

        public delegate void DataReceivedFunc(IPEndPoint endPoint, byte[] data);
        public event DataReceivedFunc DataReceivedEvent;

        public UdpManager() : this(AddressFamily.InterNetwork)
        {

        }

        public UdpManager(AddressFamily family) : this(0, family) //port 0 = any port available
        {
        }

        public UdpManager(int port) : this(port, AddressFamily.InterNetwork)
        {
        }

        public UdpManager(int port, AddressFamily family) : this(
            family == AddressFamily.InterNetwork ?
            new IPEndPoint(IPAddress.Any, port) :
            new IPEndPoint(IPAddress.IPv6Any, port))
        {
        }
        public UdpManager(string address, int port) : this(IPAddress.Parse(address), port)
        {
        }
        public UdpManager(IPAddress address, int port) : this(new IPEndPoint(address, port))
        {
        }

        //Main constructor(Called by all constructors)
        public UdpManager(IPEndPoint localEP)
        {
            RUdpClient = new NetManager(this);

            _localEP = localEP;
        }

        public void Start()
        {
            RUdpClient.Start(_localEP.Port);
            new Thread(RunEvents).Start();
        }

        private void RunEvents()
        {
            while (RUdpClient.IsRunning)
                RUdpClient.PollEvents();
        }

        #region Connect Methods
        public NetPeer Connect(string host, int port) =>
            RUdpClient.Connect(host, port, ConnectionKey);
        public NetPeer Connect(string host, int port, string key) =>
            RUdpClient.Connect(host, port, key);
        public NetPeer Connect(string host, int port, DataWriter connectionData) =>
            RUdpClient.Connect(host, port, NetDataWriter.FromBytes(connectionData.GetData(), false));

        public NetPeer Connect(IPEndPoint iPEndPoint) =>
            RUdpClient.Connect(iPEndPoint, ConnectionKey);
        public NetPeer Connect(IPEndPoint iPEndPoint, string key) =>
            RUdpClient.Connect(iPEndPoint, key);
        public NetPeer Connect(IPEndPoint iPEndPoint, DataWriter connectionData) =>
            RUdpClient.Connect(iPEndPoint, NetDataWriter.FromBytes(connectionData.GetData(), false));

        public NetPeer Connect(IPAddress host, int port) =>
            Connect(new IPEndPoint(host, port));
        public NetPeer Connect(IPAddress host, int port, string key) =>
            Connect(new IPEndPoint(host, port), key);
        public NetPeer Connect(IPAddress host, int port, DataWriter connectionData) =>
            Connect(new IPEndPoint(host, port), connectionData);
        #endregion

        public void SendToAll(object obj, DeliveryMethod deliveryMethod)
        {
            byte[] objGraph = Bytes.ClassToByte(obj);
            SendToAll(objGraph, deliveryMethod);
        }
        public void SendToAll(ISerializable obj, DeliveryMethod deliveryMethod)
        {
        //Makes a DataWriter with the object's type
            var writer = new DataWriter(obj);
            obj.Serialize(writer);
            SendToAll(writer, deliveryMethod);
        }
        public void SendToAll(DataWriter obj, DeliveryMethod deliveryMethod) =>
            SendToAll(obj.GetData(), deliveryMethod);

        public void SendToAll(byte[] data, DeliveryMethod deliveryMethod)
        {
            RUdpClient.SendToAll(data, deliveryMethod);
        }

        /// <summary>Send raw UDP packet, not reliable</summary>
        public void SendUnconnectedMessage(object obj, IPEndPoint endPoint)
        {
            byte[] objGraph = Bytes.ClassToByte(obj);
            SendUnconnectedMessage(objGraph, endPoint);
        }
        /// <summary>Send raw UDP packet, not reliable</summary>
        public void SendUnconnectedMessage(DataWriter obj, IPEndPoint endPoint) =>
            SendUnconnectedMessage(obj.GetData(), endPoint);
        /// <summary>Send raw UDP packet, not reliable</summary>
        public void SendUnconnectedMessage(byte[] data, IPEndPoint endPoint) =>
            RUdpClient.SendUnconnectedMessage(data, endPoint);

        /*
        public void Send(object obj, IPEndPoint endPoint)
        {
            byte[] objGraph = Bytes.ClassToByte(obj);
            Bytes size = objGraph.Length;
            client.SendTo(size, endPoint);
            client.SendTo(objGraph, endPoint);
        }
        public void Send(object obj, IPEndPoint[] endPoints)
        {
            foreach (IPEndPoint endPoint in endPoints)
                Send(obj, endPoint);
        }
        */

        /// <summary>Disconnects from peer</summary>
        public void Kick(NetPeer peer) =>
            RUdpClient.DisconnectPeer(peer);
        /// <summary>Disconnect from peer with no additional data</summary>
        public void ForceKick(NetPeer peer) =>
            RUdpClient.DisconnectPeerForce(peer);

        /// <summary>Disconnects all peers</summary>
        public void Close()
        {
            RUdpClient.DisconnectAll();
            RUdpClient.Stop();
        }

        /// <summary>
        /// Udp can only be of family adress InterNetwork (IPv4) or InterNetworkV6 (IPv6)
        /// </summary>
        private Socket getNewSocket(AddressFamily family = AddressFamily.InterNetwork)
        {
            return new Socket(family, SocketType.Dgram, ProtocolType.Udp);
        }

        public void Dispose()
        {
            Close();
        }
    }
}
