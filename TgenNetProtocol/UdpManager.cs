using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TgenSerializer;

namespace TgenNetProtocol
{
    public class UdpManager : IDisposable
    {

        private Socket client;
        private Socket Client => client;

        public bool Connected => EndPoint != null;

        public event UdpEvent PacketLoss;
        public delegate void UdpEvent(UdpInfo info);

        public IPEndPoint LocalEP { get; private set; }
        public IPEndPoint EndPoint { get; private set; }

        private bool active = false;
        /// <summary>Is listening to incoming packets(A thread is on the binded socket), only active if the socket is bound</summary>
        public bool Active => active && client.IsBound;

        public UdpManager() : this(AddressFamily.InterNetwork)
        {

        }

        public UdpManager(AddressFamily family) =>
            client = getNewSocket(family);

        public UdpManager(int port) : this(port, AddressFamily.InterNetwork)
        {
        }

        public UdpManager(int port, AddressFamily family)
        {
            client = getNewSocket(family);

            IPEndPoint localEndPoint;

            if (family == AddressFamily.InterNetwork)
                localEndPoint = new IPEndPoint(IPAddress.Any, port);

            else
                localEndPoint = new IPEndPoint(IPAddress.IPv6Any, port);

            Client.Bind(localEndPoint);
            LocalEP = localEndPoint;
        }
        public UdpManager(string address, int port) : this(IPAddress.Parse(address), port)
        {
        }
        public UdpManager(IPAddress address, int port) : this(new IPEndPoint(address, port))
        {
        }

        public UdpManager(IPEndPoint localEP)
        {
            client = getNewSocket(localEP.AddressFamily);
            client.Bind(localEP);
            LocalEP = localEP;
        }

        public void Bind(IPEndPoint localEP)
        {
            client.Bind(localEP);
        }

        public void Listen()
        {
            if (Active)
                return;

            active = true;
            Thread listener = new Thread(ReadPackets);
            listener.Start();
        }

        private void ReadPackets()
        {
            while (Active) //While socket is bound
            {
                UdpInfo info = new UdpInfo { Receiver = Client };
                try
                {
                    //TODO: make it IPAddress.IPv6Any when the local point family is IPv6
                    EndPoint tempRemoteEP = new IPEndPoint(IPAddress.Any, 0); //IPv4, Any port

                    byte[] sizeBuffer = new byte[sizeof(int)];
                    client.Receive(sizeBuffer);

                    int size = Bytes.B2P<int>(sizeBuffer);
                    Bytes Packet = new byte[size];
                    //Could break if Bytes returns a new byte[] when casted into byte[]
                    client.ReceiveFrom(Packet, ref tempRemoteEP);

                    info.EndPoint = (IPEndPoint)tempRemoteEP;
                    object marshallObj = Packet.ToMarshall();
                    TypeSetter.SendNewDatagramMessage(marshallObj, info);
                }
                catch (Exception e)
                {
                    PacketLoss?.Invoke(info);
                }
            }
        }

        public void Connect(string host, int port)
        {
            IPAddress address = IPAddress.Parse(host);
            IPEndPoint endPointIP = new IPEndPoint(address, port);
            client.Connect(endPointIP);
            EndPoint = endPointIP;
        }
        public void Connect(IPAddress address, int port)
        {
            IPEndPoint endPointIP = new IPEndPoint(address, port);
            client.Connect(endPointIP);
            EndPoint = endPointIP;
        }
        public void Connect(IPEndPoint iPEndPoint)
        {
            client.Connect(iPEndPoint);
            EndPoint = iPEndPoint;
        }

        public void Send(object obj)
        {
            //Will throw an error if not connected to endpoint
            byte[] objGraph = Bytes.ClassToByte(obj);
            Bytes size = objGraph.Length;
            if (objGraph.Length > ushort.MaxValue)
                return;
            //Console.WriteLine($"Size is: {size.Length} and max is: {client.SendBufferSize} is it fine? {size.Length < client.SendBufferSize}");
            client.Send(size);
            client.Send(objGraph);
        }
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

        public void Close()
        {
            active = false;
            Client.Close();
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
            client.Dispose(); //Dispose also close the socket
        }
    }
}
