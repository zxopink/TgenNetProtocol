using System;
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
        private Task pollEventsTask;
        private CancellationTokenSource cancellationToken;

        private Client client;
        public Client Client { get => client; }

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
        public bool Active   // property
        {
            get { return client; }
        }

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
            formatter = new TgenSerializer.Formatter(CompressionFormat.Binary);
        }
        public ClientManager(IFormatter formatter)
        {
            client = new Client(GetNewSocket()); //make an empty one that will be replaced for later
            this.formatter = formatter;
        }

        public bool Connected { get => client; }

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
            catch (SocketException e)
            {
                Close();
                if(throwOnError) throw;
                return false;
            }
        }

        public async Task<bool> ConnectAsync(string ip, int port) =>
            await Task.Run( () => Connect(ip, port));

        public async Task<bool> ConnectAsync(string ip, int port, string passKey) =>
            await Task.Run(() => Connect(ip, port, Bytes.StrToBytes(passKey)));

        public async Task<bool> ConnectAsync(string ip, int port, byte[] data) =>
            await Task.Run(() => Connect(ip, port, data));

        //TODO: make a proper binding
        public void Bind(IPEndPoint localEndPoint)
        {
            client.Socket.Bind(localEndPoint);
        }
        public void Close()
        {
            cancellationToken?.Cancel();
            cancellationToken?.Dispose();
            client.Close();
            client = new Client(GetNewSocket());
        }

        public void Send(object message, bool throwOnError = false)
        {
            try
            {
                if (client)
                {
                    NetworkStream stm = client;
                    Formatter.Serialize(stm, message);
                }
                else
                    throw new SocketException((int)SocketError.NotConnected);
            }
            catch (Exception e) //Usually gets thrown when the server aborted/kicked the client
            {
                Close();
                OnDisconnect?.Invoke();
                if (throwOnError)
                    throw;
            }
        }

        public void Send<T>(bool throwOnError = false) where T : new() => Send(new T());

        public void PollEvents()
        {
            if (!client) //If not connected
                return;
            try
            {
                HandlePacket();
            }
            catch (Exception)
            {
                //Packet faliure, doesn't mean the socket is unavailable
            }
            
        }

        private void HandlePacket()
        {
            NetworkStream stm = client;
            if (stm.DataAvailable && !client.IsControlled)
            {
                object message = Formatter.Deserialize(stm);
                OnPacket(message);
                CallRegisters(message);
                TypeSetter.SendNewNetMessage(message, this);
            }
        }

        /// <summary>
        /// Opens a task that automatically poll events
        /// </summary>
        /// <param name="millisecondsTimeOutPerPoll">Time to sleep between each poll</param>
        /// <returns>CancellationTokenSource, to cancel the task at any time</returns>
        public CancellationTokenSource ManagePollEvents(int millisecondsTimeOutPerPoll)
        {
            if (pollEventsTask != null)
                return cancellationToken;

            cancellationToken = new CancellationTokenSource();
            pollEventsTask = ManagePollEvents(millisecondsTimeOutPerPoll, cancellationToken.Token);
            
            return cancellationToken;
        }

        public async Task ManagePollEvents(int millisecondsTimeOutPerPoll, CancellationToken token)
        {
            while (client && !token.IsCancellationRequested)
            {
                //Make this function an automatic seperated thread poll events 
                //And take int milliseconds as an argument for a thread.sleep() to not overload the CPU
                PollEvents();
                await Task.Delay(millisecondsTimeOutPerPoll, token);
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
