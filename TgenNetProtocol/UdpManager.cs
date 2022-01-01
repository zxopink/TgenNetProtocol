using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TgenSerializer;
using RUDPSharp;

namespace TgenNetProtocol
{
    public class UdpManager : IDisposable
    {
        public bool Connected { get { try { return RUdpClient.EndPoint != null; } catch { return false;} } }
        public IPEndPoint EndPoint { get { try { return (IPEndPoint)RUdpClient.EndPoint; } catch { return null; } } }

        //Received data but failed to Deserialize it(Turn it into runtime object)
        public event UdpEvent PacketLoss;
        public delegate void UdpEvent(UdpInfo info, byte[] data);

        public RUDP<UDPSocket> RUdpClient { get; private set; }

        //Default connection key
        private const string CONN_KEY = "TgenKey";
        private string userKey = null;
        //If changed from null, will deny connections if keys don't match between peers
        public string ConnectionKey { get => userKey ?? CONN_KEY; set => userKey = value; }

        public IPEndPoint LocalEP { get; private set; }

        public delegate void DisconnectedFunc(IPEndPoint endPoint);
        public event DisconnectedFunc DisconnectedEvent;

        public delegate void PendingConnectionFunc(IPEndPoint endPoint, string keyCode, ref bool acceptConn);
        public event PendingConnectionFunc PendingConnectionEvent;

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
            RUdpClient = new RUDP<UDPSocket>(new UDPSocket("TgenSocket"));

            RUdpClient.ConnetionRequested = IncomingConnection;
            RUdpClient.DataReceived = DataReceived;
            RUdpClient.Disconnected = PeerLeft;

            LocalEP = localEP;
        }

        public void Start()
        {
            RUdpClient.Start(LocalEP.Port);
        }

        /////////////////////////////////////////////////////EVENTS////////////////////////////////////////////////////
        private void PeerLeft(EndPoint ep)
        {
            Console.WriteLine($"{ep.ToString()} has left");
            DisconnectedEvent?.Invoke((IPEndPoint)ep);
        }
        private (bool,string) IncomingConnection(EndPoint ep, byte[] data)
        {
            Console.WriteLine($"{ep} is connecting with data: {Bytes.BytesToStr(data)}");

            string pass = Bytes.BytesToStr(data);
            bool accept = (ConnectionKey) == pass;
            PendingConnectionEvent?.Invoke((IPEndPoint)ep, pass, ref accept);
            if(!accept)
                Console.WriteLine($"Dropping connection with {ep} for incorrect password\n" +
                    $"My pass: '{ConnectionKey}' got pass: '{pass}'");
            return (accept, ConnectionKey); //Accept the incoming client and send back the passcode
        }

        private bool DataReceived(EndPoint ep, byte[] data)
        {
            var reader = new DataReader(data);
            Type type = reader.TryGetType();

            UdpInfo info = new UdpInfo((IPEndPoint)ep);

            DataReceivedEvent?.Invoke((IPEndPoint)ep, data);

            try
            {
                if (type == null) //Failed to get type
                {
                    var obj = Bytes.ByteToClass(reader.GetRemainingBytes());
                    TypeSetter.SendNewDatagramMessage(obj, info);
                    return true;
                }

                else if (type.IsAssignableFrom(typeof(ISerializable)))
                {
                    var formatObj = (ISerializable)Activator.CreateInstance(type);
                    formatObj.Deserialize(reader);
                    TypeSetter.SendNewDatagramMessage(formatObj, info);
                    return true;
                }
            }
            catch //Failed to Deserialize the object, drop packet
            {
                PacketLoss?.Invoke(info, data);
                return false;
            }

            return false; //Not Serializeable object, shouldn't reach here 
            //Note: The return value shouldn't matter, it's a failed/succeeded state for me
        }

        public bool Connect(string host, int port) =>
            RUdpClient.Connect(host, port, ConnectionKey);
        public bool Connect(string host, int port, string key) =>
            RUdpClient.Connect(host, port, key);

        public bool Connect(IPAddress address, int port) =>
            Connect(address.ToString(), port);
        public bool Connect(IPAddress address, int port, string key) =>
            Connect(address.ToString(), port, key);

        public bool Connect(IPEndPoint iPEndPoint) =>
            Connect(iPEndPoint.Address, iPEndPoint.Port);
        public bool Connect(IPEndPoint iPEndPoint, string key) =>
            Connect(iPEndPoint.Address, iPEndPoint.Port, key);

        public void Send(object obj, Channel deliveryMethod)
        {
            byte[] objGraph = Bytes.ClassToByte(obj);
            Send(objGraph, deliveryMethod);
        }
        public void Send(ISerializable obj, Channel deliveryMethod)
        {
            //Makes a DataWriter with the object's type
            var writer = new DataWriter(obj);
            obj.Serialize(writer);
            Send(writer, deliveryMethod);
        }
        public void Send(DataWriter obj, Channel deliveryMethod)
        {
            Send(obj.GetData(), deliveryMethod);
        }

        public void Send(byte[] data, Channel deliveryMethod)
        {
            RUdpClient.SendToAll(deliveryMethod, data);
        }


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

        public void Close()
        {
            RUdpClient.Disconnect();
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
            RUdpClient.Dispose();
        }
    }
}
