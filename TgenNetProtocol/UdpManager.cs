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
using System.Collections.Concurrent;

namespace TgenNetProtocol
{
    public class UdpManager : IDisposable
    {
        public bool Connected { get { try { return RUdpClient.EndPoint != null; } catch { return false; } } }
        public IPEndPoint LocalEP { get { try { return (IPEndPoint)RUdpClient.EndPoint; } catch { return null; } } }
        //Used to set a listening port
        private IPEndPoint _localEP { get; set; }
        /// <summary>A UDP socket can be connected to multiple peers at once, returns all the connected peers</summary>
        public IPEndPoint[] EndPoints { get => RUdpClient.Remotes.Cast<IPEndPoint>().ToArray(); }

        //Received data but failed to Deserialize it(Turn it into runtime object)
        public event UdpEvent PacketLoss;
        public delegate void UdpEvent(UdpInfo info, byte[] data);

        public RUDP<UDPSocket> RUdpClient { get; private set; }

        //Default connection key
        private const string CONN_KEY = "TgenKey";
        private string userKey = null;
        //If changed from null, will deny connections if keys don't match between peers
        public string ConnectionKey { get => userKey ?? CONN_KEY; set => userKey = value; }

        public delegate void DisconnectedFunc(IPEndPoint endPoint);
        public event DisconnectedFunc DisconnectedEvent;

        public delegate void PendingConnectionFunc(IPEndPoint endPoint, string keyCode, ref bool acceptConn);
        public event PendingConnectionFunc PendingConnectionEvent;

        public delegate void ConnectedFunc(IPEndPoint endPoint);
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
            RUdpClient = new RUDP<UDPSocket>(new UDPSocket("TgenSocket"));

            RUdpClient.ConnetionRequested = IncomingConnection;
            RUdpClient.DataReceived = DataReceived;
            RUdpClient.Disconnected = PeerLeft;

            _localEP = localEP;
        }

        public void Start()
        {
            RUdpClient.Start(_localEP.Port);
        }

        /////////////////////////////////////////////////////EVENTS////////////////////////////////////////////////////
        private void PeerLeft(EndPoint ep)
        {
            Console.WriteLine($"{ep.ToString()} has left");
            DisconnectedEvent?.Invoke((IPEndPoint)ep);
        }
        private (bool, string) IncomingConnection(EndPoint ep, byte[] data)
        {
            Console.WriteLine($"{ep} is connecting with data: {Bytes.BytesToStr(data)}");

            string pass = Bytes.BytesToStr(data);
            bool accept = (ConnectionKey) == pass;
            PendingConnectionEvent?.Invoke((IPEndPoint)ep, pass, ref accept);
            if (!accept)
                Console.WriteLine($"Dropping connection with {ep} for incorrect password\n" +
                    $"My pass: '{ConnectionKey}' got pass: '{pass}'");
            if (accept) ConnectedEvent?.Invoke((IPEndPoint)ep);
            return (accept, ConnectionKey); //Accept the incoming client and send back the passcode
        }

        ConcurrentDictionary<DateTime, MTUPacket> MTUPackets = new ConcurrentDictionary<DateTime, MTUPacket>();
        private bool DataReceived(EndPoint ep, byte[] data)
        {
            try
            {
                var reader = new DataReader(data);
                PacketFlags flags = (PacketFlags)reader.GetByte();
                UdpInfo info = new UdpInfo((IPEndPoint)ep);

                DataReceivedEvent?.Invoke((IPEndPoint)ep, data);
                if (flags == PacketFlags.MTU)
                {
                    Console.WriteLine("Got packet");
                    var packetTime = new DateTime(reader.GetInt64());
                    if (!MTUPackets.TryGetValue(packetTime, out MTUPacket packet))
                    {
                        var size = reader.GetByte();
                        packet = new MTUPacket(size);
                        MTUPackets.TryAdd(packetTime, packet);
                    }
                    else
                    {
                        packet.Append(data);
                    }

                    if (!packet.IsComplete)
                        return true;

                    MTUPackets.TryRemove(packetTime, out packet);
                    Console.WriteLine("large packet is complete! ");
                    var obj = Bytes.ByteToClass(reader.GetBytes());
                    TypeSetter.SendNewDatagramMessage(obj, info);
                    return true;
                }

                try
                {
                    var obj = Bytes.ByteToClass(reader.GetBytes());
                    TypeSetter.SendNewDatagramMessage(obj, info);
                    return true;
                }
                catch (Exception e) //Failed to Deserialize the object, drop packet
                {
                    PacketLoss?.Invoke(info, data);
                    return false;
                    //Not Serializeable object, shouldn't reach here 
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
            }
            return false;
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

        public bool SendAll(object obj, Channel deliveryMethod = Channel.ReliableInOrder)
        {
            byte[] objGraph = Bytes.ClassToByte(obj);
            return SendAll(objGraph, deliveryMethod);
        }
        //public bool SendAll(ISerializable obj, Channel deliveryMethod = Channel.ReliableInOrder)
        //{
        //Makes a DataWriter with the object's type
        //    var writer = new DataWriter(obj);
        //    obj.Serialize(writer);
        //    return SendAll(writer, deliveryMethod);
        //}
        //public bool SendAll(DataWriter obj, Channel deliveryMethod = Channel.ReliableInOrder) =>
        //    SendAll(obj.GetData(), deliveryMethod);

        public bool SendAll(byte[] data, Channel deliveryMethod = Channel.ReliableInOrder)
        {
            PacketFlags flags = PacketFlags.None;
            if (data.Length > UDPSocket.BufferSize) //MTU (Max Transmission Unit packet)
            {
                bool flag = true;
                long packetTime = DateTime.Now.Ticks; //= packetId
                flags |= PacketFlags.MTU;

                int fragSize = UDPSocket.BufferSize - 100;
                byte fragments = (byte)(data.Length / fragSize);
                if (fragments > byte.MaxValue)
                    throw new SocketException((int)SocketError.MessageSize); //Bigger than 2GBs

                DataWriter firstPacket = new DataWriter();
                firstPacket.WriteBytes((byte)flags);
                firstPacket.WriteBytes(packetTime);
                firstPacket.WriteBytes(fragments);
                flag &= RUdpClient.SendToAll(Channel.ReliableInOrder, firstPacket.GetData());

                for (int i = 0; i < fragments; i++)
                {
                    DataWriter packet = new DataWriter();
                    /*Header*/
                    packet.WriteBytes((byte)flags);
                    packet.WriteBytes(packetTime);
                    /*Data*/
                    byte[] buffer = new byte[fragSize];
                    Buffer.BlockCopy(data, fragSize * i, buffer, 0, fragSize);
                    packet.WriteBytes(buffer);
                    flag &= RUdpClient.SendToAll(Channel.ReliableInOrder, packet.GetData());
                }
                return flag;
            }
            DataWriter writer = new DataWriter();
            writer.WriteBytes((byte)flags);
            writer.WriteBytes(data);
            return RUdpClient.SendToAll(deliveryMethod, writer.GetData());
        }

        public bool Send(EndPoint ep, object obj, Channel deliveryMethod = Channel.ReliableInOrder)
        {
            byte[] objGraph = Bytes.ClassToByte(obj);
            return Send(ep, objGraph, deliveryMethod);
        }
        //public bool Send(EndPoint ep, ISerializable obj, Channel deliveryMethod = Channel.ReliableInOrder)
        //{
            //Makes a DataWriter with the object's type
        //    var writer = new DataWriter(obj);
        //    obj.Serialize(writer);
        //    return Send(ep, writer, deliveryMethod);
        //}
        //public bool Send(EndPoint ep, DataWriter obj, Channel deliveryMethod = Channel.ReliableInOrder) =>
        //    Send(ep, obj.GetData(), deliveryMethod);

        public bool Send(EndPoint ep,byte[] data, Channel deliveryMethod = Channel.ReliableInOrder) =>
            RUdpClient.SendTo(ep,deliveryMethod, data);


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

    enum PacketFlags : byte
    {
        None = 0x00,
        MTU = 0x80
    }
}
