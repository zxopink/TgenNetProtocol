using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using TgenSerializer;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Security.Principal;
using System.Collections.ObjectModel;

namespace TgenNetProtocol
{
    public partial class ServerManager<ClientsType> : IDisposable, INetManager, IServerManager
        where ClientsType : IPeerInfo
    {
        public delegate void NetworkActivity(ClientsType client);
        public event NetworkActivity ClientDisconnectedEvent;
        public event Action<ClientsType, Exception> ClientAbortEvent;
        public event NetworkActivity ClientConnectedEvent;

        /// <param name="data">Client data when connecting</param>
        /// <param name="accept">Whether to accept the connection or not, accept is true if data and server password match</param>
        public delegate void RequestPending(Socket info, byte[] data, ref bool accept);
        public event RequestPending ClientPendingEvent;
        public event Action<Socket> ClientDeclinedEvent;
        private List<ClientsType> clients = new List<ClientsType>();
        public ReadOnlyCollection<ClientsType> Clients => clients.AsReadOnly();
        private Socket listener;
        private readonly bool dualMode; //NEW VAR

        private readonly IPEndPoint localEP; //local EndPoint
        public IFormatter Formatter { get; set; }
        public IClientsFactory<ClientsType> ClientsFactory { get; set; }
        //private bool listen = false; //made to control the listening thread

        /// <summary>
        /// Uses 'TgenSerializer' as a default Formatter
        /// </summary>
        public ServerManager(int port, IClientsFactory<ClientsType> clientsFactory)
        {
            ClientsFactory = clientsFactory;
            localEP = new IPEndPoint(IPAddress.IPv6Any, port);
            dualMode = true;

            active = false;
            Formatter = new TgenFormatter();
        }
        /// <summary>
        /// Uses 'TgenSerializer' as a default Formatter
        /// </summary>
        public ServerManager(IPAddress localaddr, int port, IClientsFactory<ClientsType> clientsFactory)
        {
            ClientsFactory = clientsFactory;
            localEP = new IPEndPoint(localaddr, port);
            dualMode = false;

            active = false;
            Formatter = new TgenFormatter();
        }
        /// <summary>
        /// Uses 'TgenSerializer' as a default Formatter
        /// </summary>
        public ServerManager(IPEndPoint localEP, IClientsFactory<ClientsType> clientsFactory)
        {
            ClientsFactory = clientsFactory;
            this.localEP = localEP;
            dualMode = false;

            active = false;
            Formatter = new TgenFormatter();
        }

        public ServerManager(int port, IFormatter formatter, IClientsFactory<ClientsType> clientsFactory)
        {
            ClientsFactory = clientsFactory;
            localEP = new IPEndPoint(IPAddress.IPv6Any, port);
            dualMode = true;

            active = false;
            this.Formatter = formatter;
        }
        public ServerManager(IPAddress localaddr, int port, IFormatter formatter, IClientsFactory<ClientsType> clientsFactory)
        {
            ClientsFactory = clientsFactory;
            localEP = new IPEndPoint(localaddr, port);
            dualMode = false;

            active = false;
            this.Formatter = formatter;
        }
        public ServerManager(IPEndPoint localEP, IFormatter formatter, IClientsFactory<ClientsType> clientsFactory)
        {
            ClientsFactory = clientsFactory;
            this.localEP = localEP;
            dualMode = false;

            active = false;
            this.Formatter = formatter;
        }

        private bool active;

        /// <summary>Returns if the listener is active, not related to states with clients</summary>
        public bool Active => active;
        public int AmountOfClients => clients.Count;

        public string PublicIp
        {
            get { try { return new WebClient().DownloadString("http://icanhazip.com"); } catch(Exception) { return "Unable to load public IP"; } }
        }

        public int Port => localEP.Port;
        public IPAddress Address => localEP.Address;
        public AddressFamily AddressFamily => localEP.AddressFamily;

        public const int PASSKEY_TIMEOUT = 5000;
        private byte[] passKey;
        public string PassKeyStr { 
            get => passKey != null ? Bytes.BytesToStr(passKey) : null; 
            set => passKey = value != null ? Bytes.StrToBytes(value) : null;
        }
        /// <summary> Key to accept incoming connections, can be set as string by `PassKeyStr` property.
        /// If set to null, every connection will be accepted </summary>
        public byte[] PassKey { get => passKey; set => passKey = value; }

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
                //throw new Exception("No network adapters with an IPv4 address in the system!");
            }
        }
        private Socket GetNewListenSocket()
        {
            Socket socket = new Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            socket.DualMode = dualMode;
            socket.Bind(localEP);
            return socket;
        }

        private async void AcceptIncomingClient()
        {
            Socket socket = listener.Accept();
            socket.NoDelay = true; //disables delay which occures when sending small chunks of data
            bool approved = await CheckPass(socket);

            if (!approved)
            {
                ClientDeclinedEvent?.Invoke(socket);
                socket.Send(new byte[] { 0 /*FAILED*/});
                socket.Close();
                return;
            }
            socket.Send(new byte[] { 200 /*200 OK*/});

            ClientsType client = ClientsFactory.PeerConnection(socket, this);
            clients.Add(client);
            ClientConnectedEvent?.Invoke(client);
        }

        public async Task<bool> CheckPass(Socket s)
        {
            if (passKey == null)
                return true;

            bool result = false;
            ArraySegment<byte> seg = new ArraySegment<byte>(new byte[passKey.Length]);
            Task<int> readPass = s.ReceiveAsync(seg, SocketFlags.None);
            if (await Task.WhenAny(readPass, Task.Delay(PASSKEY_TIMEOUT)) == readPass)
            {
                // task completed within timeout
                bool flag = true;
                byte[] packet = seg.Array;
                for (int i = 0; i < passKey.Length; i++)
                    if (passKey[i] != packet[i])
                        flag = false; //Password wrong

                result = flag; //Password right if flag is true
            }
            else
            {
                // timeout logic
                result = false;
            }

            ClientPendingEvent?.Invoke(s, seg.Array, ref result); //`result` can be changed by event
            return result;
        }

        private void HandleClientPacket(ClientsType client)
        {
            NetworkStream stm = client.NetworkStream;
            try
            {
                if (!stm.DataAvailable) return;
                object message = Formatter.Deserialize(stm);
                OnPacket(message, client);
                CallRegisters(message, client);
                TypeSetter.SendNewNetMessage(message, this, client);
            }
            //the listener WILL crash when client hangs the server
            //the catch makes sure to handle the program properly when a client leaves
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException)
            {
                //Methods mismatch, critical issue
                //not related to client
                throw;
            }
            catch (Exception e)
            {
                if(clients.Contains(client))
                    AbortClient(client, e);
            }
        }

        /// <summary>Accepts awaiting clients and invokes callbacks based on incoming packets.
        /// Should be used inside an infinite loop or call the `ManagePollEvents` to handle asynchronously</summary>
        public void PollEvents()
        {
            while (listener.Poll(0, SelectMode.SelectRead))//Equivelent to `TcpListener.Pending()`
                AcceptIncomingClient(); //Method also adds the client to the clients list

            for (int i = 0; i < AmountOfClients; i++)
                HandleClientPacket(clients[i]);
        }

        /// <summary>Stop and drop communications with a client</summary>
        /// <param name="client">The id of the client</param>
        public void KickClient(ClientsType client)
        {
            Socket socket = client.Socket;
            bool removed = clients.Remove(client);
            socket.Close();
            if(removed)
                ClientDisconnectedEvent?.Invoke(client);
        }

        /// <summary>Stop and drop communications with everyone connected</summary>
        public void KickAll()
        {
            for (int i = AmountOfClients - 1; i >= 0; i--)
                KickClient(clients[i]);
        }

        /// <summary>
        /// The Server uses this function to drop clients who threw an exception
        /// (Clients that disconnected/had a socket error)
        /// </summary>
        /// <param name="client"></param>
        private void AbortClient(ClientsType client, Exception error)
        {
            ClientAbortEvent?.Invoke(client, error);
            KickClient(client);
        }

        /// <summary>
        /// Sends a message to a specific client based on the client's ID
        /// </summary>
        /// <param name="Message">The message you want to send</param>
        /// <param name="client">The id of the client who's supposed to get the message</param>
        /// <param name="throwOnError">Throw exception on failed send</param>
        public void Send(object Message, ClientsType client, bool throwOnError = false)
        {
            try
            {
                NetworkStream stm = client.NetworkStream;
                Formatter.Serialize(stm, Message);
            }
            catch (SerializationException) { throw; } //Message cannot be serialized
            catch (Exception e) 
            {
                if(clients.Contains(client)) AbortClient(client, e);
                if (throwOnError) throw; /*client left as the message was serialized*/ 
            }
        }

        public void Send(object Message, IEnumerable<ClientsType> clients, bool throwOnError = false)
        {
            foreach (var client in clients)
                Send(Message, client, throwOnError);
        }
        public void Send(object Message, params ClientsType[] clients) => 
            Send(Message, clients, false);

        /// <summary>
        /// Sends a message to all connected client,
        /// </summary>
        /// <param name="Message">The message you want to send</param>
        /// <param name="throwOnError">Throw exception on failed send</param>
        public void SendToAll(object Message, bool throwOnError = false)
        {
            if (clients.Count >= 0)
            {
                for (int i = 0; i < AmountOfClients; i++)
                {
                    ClientsType client = clients[i];
                    Send(Message, client, throwOnError);
                }
            }
        }

        /// <summary>
        /// Sends a message to all connected clients except for the client,
        /// common feature among chats
        /// </summary>
        /// <param name="Message">The message</param>
        /// <param name="client">The client that won't get the message</param>
        /// /// <param name="throwOnError">Throw exception on failed send</param>
        public void SendToAllExcept(object Message, ClientsType client, bool throwOnError = false)
        {
            for (int i = 0; i < AmountOfClients; i++)
            {
                ClientsType currentClient = clients[i];
                if (!currentClient.Equals(client))
                    Send(Message, currentClient, throwOnError);
            }
        }

        public ServerManager<ClientsType> Start() => 
            Start((int)SocketOptionName.MaxConnections);

        public ServerManager<ClientsType> Start(int backlog)
        {
            if (!active)
            {
                active = true;
                listener = GetNewListenSocket();
                listener.Listen(backlog);
            }
            return this;
        }

        /// <summary>Opens a task that polls events until the listener is closed and no clients are connected</summary>
        /// <param name="millisecondsInterval">Milliseconds to sleep between each poll</param>
        /// <returns>CancellationTokenSource, to cancel the task at any time</returns>
        public Task ManagePollEvents(int millisecondsInterval) =>
            ManagePollEvents(TimeSpan.FromMilliseconds(millisecondsInterval));

        /// <summary>Opens a task that polls events until the listener is closed and no clients are connected</summary>
        /// <param name="interval">Time to sleep between each poll</param>
        /// <returns>CancellationTokenSource, to cancel the task at any time</returns>
        public async Task ManagePollEvents(TimeSpan interval)
        {
            while (Active || AmountOfClients > 0)
            {
                PollEvents();
                await Task.Delay(interval);
            }
        }

        public Task ManagePollEvents(int millisecondsInterval, CancellationToken token) =>
            ManagePollEvents(TimeSpan.FromMilliseconds(millisecondsInterval), token);

        public async Task ManagePollEvents(TimeSpan interval, CancellationToken token)
        {
            while ((Active || AmountOfClients > 0) && !token.IsCancellationRequested)
            {
                PollEvents();
                await Task.Delay(interval, token);
            }
        }

        /// <summary>
        /// Stops the listener but keeps all the connected clients.
        /// Will not accept any incoming clients
        /// </summary>
        public void Stop()
        {
            if (active && listener != null)
            {
                active = false;
                listener.Close();
            }
        }
        /// <summary>
        /// Stops the listener then aborts all the connected client before it aborts the thread that listens to incoming clients
        /// </summary>
        public void Close()
        {
            if (active && listener != null)
                Stop();

            KickAll();
        }

        /// <summary>
        /// Closes the server
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
    /// <summary>Used for TypeSetter to identify the ServerManager because it's generic</summary>
    internal interface IServerManager { }
}
