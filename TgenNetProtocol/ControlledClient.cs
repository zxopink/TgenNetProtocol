using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using TgenSerializer;

namespace TgenNetProtocol
{
    public class ControlledClient : IDisposable
    {
        private Client client;

        public delegate void ReceivedMessage(byte[] packet, Client client);
        /// <summary>
        /// Fires an event each time a packet is received, Only works if called the HandlePackets method!
        /// </summary>
        public event ReceivedMessage ReceivedPacket;

        private bool handling;
        /// <summary>
        /// Is the instance handling packets
        /// </summary>
        public bool IsHandlingPackets { get => handling; }

        /// <summary>
        /// Is the instance active or disposed
        /// </summary>
        public bool Active { get => client; }

        public ControlledClient(Client client)
        {
            this.client = client;
            handling = false;
            //readState = null;
        }

        /// <summary>
        /// Sends any primitive type (int, string, byte) and byte array (byte[])
        /// </summary>
        /// <param name="obj"></param>
        public void Send(Bytes obj)
        {
            if (!client) throw new SocketException();

            NetworkStream stream = client;
            byte[] packet = obj.Buffer;
            stream.Write(packet, 0, packet.Length);
        }
        public void Send(byte[] packet)
        {
            if (!client) throw new SocketException();

            NetworkStream stream = client;
            stream.Write(packet, 0, packet.Length);
        }

        public byte[] Read(int length)
        {
            if (!client) throw new SocketException();

            NetworkStream stream = client;
            BinaryReader reader = new BinaryReader(stream);
            return reader.ReadBytes(length);
        }

        //IAsyncResult readState;

        public void HandlePackets()
        {
            if (handling || !client) return;
            handling = true;
            GetPackets();
        }

        private async void GetPackets()
        {
            NetworkStream stream = client;
            byte[] packet = new byte[client.Available];
            await stream.ReadAsync(packet, 0, packet.Length);
            if (!handling) return;
            ReceivedPacket?.Invoke(packet, client);
            GetPackets();
        }

        /*
        private void FirePacket(IAsyncResult ar)
        {
            if (!handling || !client) return;
            byte[] packet = (byte[])ar.AsyncState;
            ReceivedPacket?.Invoke(packet, client);
            handling = false;
            HandlePackets();
        }
        */
        private void StopHandlePackets()
        {
            //if (!handling) return;
            Console.WriteLine("cancel");
            handling = false;
            //readState = null;
        }

        public void Dispose()
        {
            StopHandlePackets();
            handling = false;
        }
    }
}
