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
using System.Runtime.Serialization;

namespace TgenNetProtocol.Udp
{
    public partial class UdpManager : INetManager
    {
        public bool IsRunning => RUdpClient.IsRunning;
        public IPEndPoint LocalEP => _localEP;
        /// <summary>Local Port</summary>
        public int Port => _localEP.Port;
        //Used to set a listening port
        private IPEndPoint _localEP { get; set; }
        public TgenFormatter Formatter { get; set; }

        public bool NatPunchEnabled { get => RUdpClient.NatPunchEnabled; set => RUdpClient.NatPunchEnabled = value; }
        public int DisconnectTimeout { get => RUdpClient.DisconnectTimeout; set => RUdpClient.DisconnectTimeout = value; }
        public NatPunchModule NatPunchModule { get => RUdpClient.NatPunchModule; }

        //Received data but failed to Deserialize it(Turn it into runtime object)
        public event UdpEvent PacketLoss;
        public delegate void UdpEvent(UdpInfo info, byte[] data);

        public NetManager RUdpClient { get; private set; }

        private CancellationTokenSource TokenSource { get; set; }
        private Task PollEventsAsync { get; set; }
        //Default connection key
        private const string CONN_KEY = "TgenKey";
        private string userKey = null;
        //If changed from null, will deny connections if keys don't match between peers
        public string ConnectionKey { get => userKey ?? CONN_KEY; set => userKey = value; }

        public delegate void DisconnectedFunc(NetPeer peer, DisconnectInfo disconnectInfo);
        public event DisconnectedFunc PeerDisconnectedEvent;

        public delegate void PendingConnectionFunc(ConnectionRequest request);
        public event PendingConnectionFunc ConnectionRequestEvent;

        public event Action<NetPeer, int> NetworkLatencyUpdateEvent;
        public event Action<IPEndPoint, SocketError> NetworkErrorEvent;

        public delegate void ConnectedFunc(NetPeer peer);
        ///<summary>Fires when a pending connection was accepted</summary>
        public event ConnectedFunc PeerConnectedEvent;

        public delegate void DataReceivedFunc(IPEndPoint endPoint, byte[] data);
        public event DataReceivedFunc OnDataReceived;

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
            Formatter = new TgenFormatter();
        }

        /// <summary>Start logic thread and listening on selected port</summary>
        public bool Start() =>
            RUdpClient.Start(_localEP.Port);

        public void PollEvents()
        {
            RUdpClient.PollEvents();
            if (NatPunchEnabled)
                NatPunchModule.PollEvents();
        }

        /// <summary>
        /// Fires a task that polls events async until the instance stops or the cancellation token is called
        /// </summary>
        /// <param name="millisecondsTimeOutPerPoll"></param>
        /// <returns>Token source to cancel the task</returns>
        public Task ManagePollEvents(int millisecondsInterval) =>
            ManagePollEvents(TimeSpan.FromMilliseconds(millisecondsInterval));

        public async Task ManagePollEvents(TimeSpan interval)
        {
            while (RUdpClient.IsRunning)
            {
                PollEvents();
                await Task.Delay(interval);
            }
        }

        public Task ManagePollEvents(int millisecondsInterval, CancellationToken token) =>
            ManagePollEvents(TimeSpan.FromMilliseconds(millisecondsInterval), token);

        public async Task ManagePollEvents(TimeSpan interval, CancellationToken token)
        {
            while (RUdpClient.IsRunning && !token.IsCancellationRequested)
            {
                PollEvents();
                await Task.Delay(interval, token);
            }
        }

        #region Connect Methods
        public virtual NetPeer Connect(string host, int port) =>
            RUdpClient.Connect(host, port, ConnectionKey);
        public virtual NetPeer Connect(string host, int port, string key) =>
            RUdpClient.Connect(host, port, key);

        public virtual NetPeer Connect(IPEndPoint iPEndPoint) =>
            RUdpClient.Connect(iPEndPoint, ConnectionKey);
        public virtual NetPeer Connect(IPEndPoint iPEndPoint, string key) =>
            RUdpClient.Connect(iPEndPoint, key);

        public virtual NetPeer Connect(IPAddress host, int port) =>
            Connect(new IPEndPoint(host, port));
        public virtual NetPeer Connect(IPAddress host, int port, string key) =>
            Connect(new IPEndPoint(host, port), key);
        #endregion

        public void SendToAll(object obj, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            byte[] objGraph = Formatter.Serialize(obj);
            SendToAll(objGraph, deliveryMethod);
        }

        public void SendToAll(byte[] data, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            RUdpClient.SendToAll(data, deliveryMethod);
        }

        public void SendToAllExcept(object obj, NetPeer exclude, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            byte[] objGraph = Formatter.Serialize(obj);
            SendToAllExcept(objGraph, exclude, deliveryMethod);
        }

        public void SendToAllExcept(byte[] data, NetPeer exclude, DeliveryMethod deliveryMethod = DeliveryMethod.ReliableOrdered)
        {
            RUdpClient.SendToAll(data, deliveryMethod, exclude);
        }

        /// <summary>Send raw UDP packet, not reliable</summary>
        public void SendUnconnectedMessage(object obj, IPEndPoint endPoint)
        {
            byte[] objGraph = Formatter.Serialize(obj);
            SendUnconnectedMessage(objGraph, endPoint);
        }
        /// <summary>Send raw UDP packet, not reliable</summary>
        public void SendUnconnectedMessage(byte[] data, IPEndPoint endPoint) =>
            RUdpClient.SendUnconnectedMessage(data, endPoint);

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

        public void Dispose()
        {
            Close();
        }
    }
}
