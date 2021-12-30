using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TgenSerializer;
using LiteNetLib;
using LiteNetLib.Utils;

namespace TgenNetProtocol
{
    public class UdpManager : IDisposable
    {
        internal EventBasedNetListener NetListener { get; private set; }
        internal NetManager RUdpClient { get; private set; }

        public bool Connected => EndPoint != null;

        public event UdpEvent PacketLoss;
        public delegate void UdpEvent(UdpInfo info);

        public IPEndPoint LocalEP { get; private set; }
        public IPEndPoint EndPoint { get; private set; }

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
            NetListener = new EventBasedNetListener();
            RUdpClient = new NetManager(NetListener);

            LocalEP = localEP;
        }

        public void Bind(IPEndPoint localEP)
        {

        }

        public void Listen()
        {
            RUdpClient.Start(LocalEP.Address.MapToIPv4(), LocalEP.Address.MapToIPv6(), LocalEP.Port);
        }
        const string CONN_KEY = "TgenKey";
        public void Connect(string host, int port, string key = null)
        {
            key = key == null ? CONN_KEY : key;
            IPAddress address = IPAddress.Parse(host);
            IPEndPoint endPointIP = new IPEndPoint(address, port);
            Connect(endPointIP, NetDataWriter.FromString(key));
        }
        public void Connect(IPEndPoint iPEndPoint, NetDataWriter dataWriter)
        {
            RUdpClient.Connect(iPEndPoint, dataWriter);
            EndPoint = iPEndPoint;
        }

        public void Send(object obj, DeliveryMethod deliveryMethod)
        {
            //Will throw an error if not connected to endpoint
            byte[] objGraph = Bytes.ClassToByte(obj);
            Bytes size = objGraph.Length;
            //if (objGraph.Length > ushort.MaxValue)
            //    return;
            //Console.WriteLine($"Size is: {size.Length} and max is: {client.SendBufferSize} is it fine? {size.Length < client.SendBufferSize}");
            RUdpClient.SendToAll(size, deliveryMethod);
        }
        public void Send(DataWriter obj, DeliveryMethod deliveryMethod)
        {
            //if (obj.data.Length > ushort.MaxValue)
            //    return;
            RUdpClient.SendToAll(obj.GetData(), deliveryMethod);
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
            RUdpClient.Stop();
        }
    }
}
