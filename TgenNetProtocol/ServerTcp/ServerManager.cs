using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using TgenSerializer;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace TgenNetProtocol
{
    public partial class ServerManager<ClientsType> : IDisposable, INetManager, IServerManager
        where ClientsType : IPeerInfo
    {
        private Task pollEventsTask;
        private CancellationTokenSource cancellationToken;

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
            Formatter = new TgenSerializer.Formatter();
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
            Formatter = new TgenSerializer.Formatter();
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
            Formatter = new TgenSerializer.Formatter();
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

        private bool active; // field

        /// <summary>Returns if the listener is active, not related to states with clients</summary>
        public bool Active => active;   // property
        public int AmountOfClients => clients.Count; // property

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

        private int clientsCount = 0; //an Id counter
        private Socket AcceptIncomingClient()
        {
            Socket clientSocket = listener.Accept();

            clientSocket.NoDelay = true; //disables delay which occures when sending small chunks of data

            Task<bool> passCheck = CheckPass(clientSocket);
            checkPassList.Add((passCheck, clientSocket));

            return clientSocket;
        }

        private void AddApprovedClients((Task<bool> passCheck, Socket socket) data)
        {
            Task<bool> check = data.passCheck;
            if (!check.IsCompleted)
                return;

            checkPassList.Remove(data);

            Socket socket = data.socket;
            bool approved = check.Result;
            if(!approved)
            {
                ClientDeclinedEvent?.Invoke(socket);
                socket.Send(new byte[] { 0 /*FAILED*/});
                socket.Close();
                return;
            }

            socket.Send(new byte[] { 200 /*200 OK*/});


            ClientsType client = ClientsFactory.PeerConnection(socket);
            clients.Add(client);
            ClientConnectedEvent?.Invoke(client);
        }

        List<(Task<bool> passCheck, Socket client)> checkPassList = new List<(Task<bool>, Socket)>();
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
            catch (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException e)
            {
                //Methods mismatch, critical issue
                throw;
            }
            catch (Exception e)
            {
                AbortClient(client, e);
            }
        }

        public void PollEvents()
        {
            while (listener.Poll(0, SelectMode.SelectRead))//Equivelent to listener.Pending() (TcpListener.Pending())
                AcceptIncomingClient(); //Method also adds the client to the clients list

            for (int i = checkPassList.Count - 1; i >= 0; i--)
                AddApprovedClients(checkPassList[i]);

            //Amount of clients can change during tick
            //int currentClients = AmountOfClients; //this value holds the connected clients during the tick
            for (int i = 0; i < AmountOfClients; i++) //AmountOfClients = length of clients list
            {
                ClientsType client = clients[i];
                HandleClientPacket(client);
            }
        }

        /// <summary>
        /// Stop and drop communications with a client
        /// </summary>
        /// <param name="client">The id of the client</param>
        public void KickClient(ClientsType client)
        {
            Socket socket = client.Socket;
            bool removed = clients.Remove(client);
            socket.Close();
            if(removed)
                ClientDisconnectedEvent?.Invoke(client);
        }

        /// <summary>
        /// The Server uses this function to drop inactive clients
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
            catch (Exception e) { if (throwOnError) throw; /*client left as the message was serialized*/ }
        }

        public void Send(object Message, IEnumerable<ClientsType> clients, bool throwOnError = false)
        {
            foreach (var client in clients)
                Send(Message, client, throwOnError);
        }

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

        public void Start() => 
            Start((int)SocketOptionName.MaxConnections);

        public void Start(int backlog)
        {
            if (!active)
            {
                active = true;
                listener = GetNewListenSocket();
                listener.Listen(backlog);
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
            while (!token.IsCancellationRequested)
            {
                //Make this function an automatic seperated thread poll events 
                //And take int milliseconds as an argument for a thread.sleep() to not overload the CPU
                PollEvents();
                await Task.Delay(millisecondsTimeOutPerPoll, token);
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
            try
            {
                if (active && listener != null)
                {
                    Stop();
                    for (int i = 0; i < AmountOfClients; i++)
                    {
                        ClientsType client = clients[i];
                        KickClient(client);
                    }
                    cancellationToken?.Cancel();
                    cancellationToken?.Dispose();
                }
            }
            catch (Exception)
            {

            }
        }

        /// <summary>
        /// Closes the server
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
    internal interface IServerManager { } //Used for TypeSetter to identify the ServerManager because it's generic
}
