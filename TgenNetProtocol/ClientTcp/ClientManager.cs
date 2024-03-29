﻿using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using TgenSerializer;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace TgenNetProtocol
{
    public partial class ClientManager : IDisposable, INetManager
    {
        private Client client;
        internal Client Client => client;

        //public event EventHandler OnConnect;
        public delegate void ClientActivity();
        public event ClientActivity OnConnect;
        /// <summary>
        /// On connection aborted
        /// </summary>
        public event ClientActivity OnDisconnect;

        private IFormatter formatter;
        public IFormatter Formatter { get => formatter; }

        /// <summary>
        /// Checks if the listener for messages is active
        /// </summary>
        public bool Active => client; // property

        public string PublicIp
        {
            get { try { return new WebClient().DownloadString("http://icanhazip.com"); } catch (Exception) { return "Unable to load public IP"; } }
        }

        /// <summary>Returns a new socket of the protocol's type</summary>
        private Socket GetNewSocket()
        {
            return new Socket(SocketType.Stream, ProtocolType.Tcp);
        }

        /// <summary>
        /// returns IPv4 if available. Else - string.Empty
        /// </summary>
        public string LocalIp
        {
            get
            {
                var host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (var ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                    {
                        return ip.ToString();
                    }
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Uses 'TgenSerializer' as a default Formatter
        /// </summary>
        public ClientManager()
        {
            client = new Client(GetNewSocket()); //make an empty one that will be replaced for later
            formatter = new TgenFormatter();
        }
        public ClientManager(IFormatter formatter)
        {
            client = new Client(GetNewSocket()); //make an empty one that will be replaced for later
            this.formatter = formatter;
        }

        public bool Connected => Client.Connected;

        /// <summary>
        /// Connects the client to the server based on the given Ip and Port
        /// </summary>
        /// <param name="ip">The server Ip</param>
        /// <param name="port">The port</param>
        /// <returns>if connected successfully returns true, else false</returns>
        public bool Connect(string ip, int port, bool throwOnError = false) =>
            Connect(ip, port, data: null, throwOnError);

        public bool Connect(string ip, int port, string passKey, bool throwOnError = false) =>
            Connect(ip, port, data: Bytes.StrToBytes(passKey), throwOnError);

        /// <summary>
        /// Connects the client to the server based on the given Ip and Port
        /// </summary>
        /// <param name="ip">The server Ip</param>
        /// <param name="port">The port</param>
        /// <returns>if connected successfully returns true, else false</returns>
        public bool Connect(string ip, int port, byte[] data, bool throwOnError = false)
        {
            if (client)
                throw new SocketException((int)SocketError.IsConnected);

            try
            {
                client.Connect(ip, port);

                if(data != null)
                    client.Socket.Send(data);

                int timeout = client.Socket.ReceiveTimeout;
                client.Socket.ReceiveTimeout = 5000;

                var result = new byte[1];
                client.Socket.Receive(result);
                if (result[0] != 200)
                    throw new SocketException((int)SocketError.ConnectionRefused);

                client.Socket.ReceiveTimeout = timeout;
                OnConnect?.Invoke();
                return true;
            }
            catch (SocketException)
            {
                Close();
                if(throwOnError) throw;
                return false;
            }
        }

        public async Task<bool> ConnectAsync(string ip, int port, bool throwOnError = false) =>
            await Task.Run( () => Connect(ip, port, throwOnError));

        public async Task<bool> ConnectAsync(string ip, int port, string passKey, bool throwOnError = false) =>
            await Task.Run(() => Connect(ip, port, Bytes.StrToBytes(passKey), throwOnError));

        public async Task<bool> ConnectAsync(string ip, int port, byte[] data, bool throwOnError = false) =>
            await Task.Run(() => Connect(ip, port, data, throwOnError));

        //TODO: make a proper binding
        public void Bind(IPEndPoint localEndPoint)
        {
            client.Socket.Bind(localEndPoint);
        }
        public void Close()
        {
            client.Close();
            client = new Client(GetNewSocket());
        }

        public void Send(object message, bool throwOnError = false)
        {
            try
            {
                if (client)
                {
                    try
                    {
                        NetworkStream stm = client;
                        Formatter.Serialize(stm, message);
                    }
                    catch (Exception)
                    {
                        Close();
                        OnDisconnect?.Invoke();
                        throw;
                    }
                }
                else
                    throw new SocketException((int)SocketError.NotConnected);
            }
            catch (Exception) //Usually gets thrown when the server aborted/kicked the client
            {
                if (throwOnError)
                    throw;
            }
        }

        public void Send<T>(bool throwOnError = false) where T : new() => Send(new T());

        /// <summary>Invokes callbacks based on incoming packets.
        /// Should be used inside an infinite loop or call the `ManagePollEvents` to handle asynchronously</summary>
        public void PollEvents()
        {
            if (!client) //If not connected
                return;
            try
            {
                HandlePacket();
            }
            catch (SocketException)
            {
                Close();
                OnDisconnect?.Invoke();
                throw;
            }    
        }

        private void HandlePacket()
        {
            NetworkStream stm = client;
            if (stm.DataAvailable)
            {
                object message = Formatter.Deserialize(stm);
                OnPacket(message);
                CallRegisters(message);
                TypeSetter.SendNewNetMessage(message, this);
            }
        }

        /// <summary>Starts a task that ends once the client instance is closed</summary>
        /// <param name="interval">Time to sleep between each poll</param>
        /// <returns>CancellationTokenSource, to cancel the task at any time</returns>
        public async Task ManagePollEvents(TimeSpan interval)
        {
            while (client.Connected)
            {
                PollEvents();
                await Task.Delay(interval);
            }
        }

        /// <summary>Starts a task that ends once the client instance is closed</summary>
        /// <param name="millisecondsInterval">Milliseconds to sleep between each poll</param>
        /// <returns>CancellationTokenSource, to cancel the task at any time</returns>
        public Task ManagePollEvents(int millisecondsInterval) =>
            ManagePollEvents(TimeSpan.FromMilliseconds(millisecondsInterval));

        public async Task ManagePollEvents(TimeSpan interval, CancellationToken token)
        {
            while (client.Connected && !token.IsCancellationRequested)
            {
                PollEvents();
                await Task.Delay(interval, token);
            }
        }

        public Task ManagePollEvents(int millisecondsInterval, CancellationToken token) =>
            ManagePollEvents(TimeSpan.FromMilliseconds(millisecondsInterval), token);

        public void Dispose()
        {
            Close();
        }
    }
}
