using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using TgenSerializer;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public class ClientManager : IDisposable
    {
        private Task pollEventsTask;
        private CancellationTokenSource cancellationToken;

        private Client client;
        public Client Client { get => client; }
        private Thread MessageListener;

        //public event EventHandler OnConnect;
        public delegate void ClientActivity();
        public event ClientActivity OnConnect;
        /// <summary>
        /// On connection aborted
        /// </summary>
        public event ClientActivity OnDisconnect;

        private Formatter formatter;
        public Formatter Formatter { get => formatter; }

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
        private Socket getNewSocket 
        {
            get { return new Socket(SocketType.Stream, ProtocolType.Tcp); }
        }

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
                return "No network adapters with an IPv4 address in the system!";
            }
        }

        public ClientManager()
        {
            client = (Client)getNewSocket; //make an empty one that will be replaced for later
            formatter = new Formatter(CompressionFormat.Binary);
        }
        public ClientManager(Formatter formatter)
        {
            client = (Client)getNewSocket; //make an empty one that will be replaced for later
            this.formatter = formatter;
        }

        public bool Connected { get => client; }

        /// <summary>
        /// This bool sets attempts, if set to true the client will attempt to connect the server 4 times before giving up else the client only tries 1 time
        /// default is False
        /// </summary>
        public bool makeAttempts = false;
        int attemptCounter = 0;
        int maxAttemptCount = 4;

        /// <summary>
        /// Connects the client to the server based on the given Ip and Port
        /// </summary>
        /// <param name="ip">The server Ip</param>
        /// <param name="port">The port</param>
        /// <returns>if connected successfully returns true, else false</returns>
        public bool Connect(string ip, int port) =>
            Connect(ip, port, data: null);

        public bool Connect(string ip, int port, string passKey) =>
            Connect(ip, port, Bytes.StrToBytes(passKey));

        /// <summary>
        /// Connects the client to the server based on the given Ip and Port
        /// </summary>
        /// <param name="ip">The server Ip</param>
        /// <param name="port">The port</param>
        /// <returns>if connected successfully returns true, else false</returns>
        public bool Connect(string ip, int port, byte[] data)
        {
            if (client)
            {
                TgenLog.Log("Client is already connected to a server!");
                return true;
            }

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
                attemptCounter = 0;
                OnConnect?.Invoke();
                return true;
            }
            catch (SocketException e)
            {
                TgenLog.Log(e.ToString());
                Close();

                if (!makeAttempts)
                    return false;

                attemptCounter++;
                Console.WriteLine("Attempt number " + attemptCounter + " to connect the server");
                if (attemptCounter == maxAttemptCount)
                {
                    attemptCounter = 0;
                    Console.WriteLine("Was not able to connect the server after " + maxAttemptCount + " attempts");
                    return false;
                }
                else
                    return Connect(ip, port);
            }
        }

        public async Task<bool> ConnectAsync(string ip, int port) =>
            await Task.Run( () => { return Connect(ip, port); } );

        public async Task<bool> ConnectAsync(string ip, int port, string passKey) =>
            await Task.Run(() => { return Connect(ip, port, Bytes.StrToBytes(passKey)); });

        public async Task<bool> ConnectAsync(string ip, int port, byte[] data) =>
            await Task.Run(() => { return Connect(ip, port, data); });

        //TODO: make a proper binding
        public void Bind(IPEndPoint localEndPoint)
        {
            ((Socket)client).Bind(localEndPoint);
        }
        public void Close()
        {
            client.Close();
            client = (Client)getNewSocket;
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
                    Console.WriteLine("The client isn't connected to a server!");
            }
            catch (Exception e) //Usually gets thrown when the server aborted/kicked the client
            {
                Close();
                OnDisconnect?.Invoke();
                TgenLog.Log(e.ToString());
                if (throwOnError)
                    throw e;
            }
        }

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
                OnDisconnect?.Invoke();
            }
            
        }

        private void HandlePacket()
        {
            NetworkStream stm = client;
            if (stm.DataAvailable && !client.IsControlled)
            {
                object message = Formatter.Deserialize(stm);
                TypeSetter.SendNewClientMessage(message);
            }
        }

        private void ManageClient(int millisecondsTimeOutPerPoll, CancellationToken token)
        {
            while (client && !token.IsCancellationRequested)
            {
                //Make this function an automatic seperated thread poll events 
                //And take int milliseconds as an argument for a thread.sleep() to not overload the CPU
                PollEvents();
                Thread.Sleep(millisecondsTimeOutPerPoll);
            }
            pollEventsTask = null;
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
            pollEventsTask = new Task(() => ManageClient(millisecondsTimeOutPerPoll, cancellationToken.Token), cancellationToken.Token);
            pollEventsTask.Start();
            
            return cancellationToken;
        }

        public void Dispose()
        {
            Close();
        }
    }
}
