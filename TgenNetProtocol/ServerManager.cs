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
    public class ServerManager : IDisposable
    {
        private Task pollEventsTask;
        private CancellationTokenSource cancellationToken;

        public delegate void NetworkActivity(ClientInfo client);
        public event NetworkActivity ClientDisconnectedEvent;
        public event NetworkActivity ClientConnectedEvent;

        /// <param name="data">Client data when connecting</param>
        /// <param name="accept">Whether to accept the connection or not, accept is true if data and server password match</param>
        public delegate void RequestPending(ClientInfo info, byte[] data, ref bool accept);
        public event RequestPending ClientPendingEvent;
        public event NetworkActivity ClientDeclineEvent;
        private List<ClientInfo> clients = new List<ClientInfo>();
        private Socket listener;
        private readonly bool dualMode; //NEW VAR

        private readonly IPEndPoint localEP; //local EndPoint
        private IFormatter formatter;
        public IFormatter Formatter { get => formatter; set => formatter = value; }
        //private bool listen = false; //made to control the listening thread

        /// <summary>
        /// Uses 'TgenSerializer' as a default Formatter
        /// </summary>
        public ServerManager(int port)
        {
            localEP = new IPEndPoint(IPAddress.IPv6Any, port);
            dualMode = true;

            active = false;
            formatter = new TgenSerializer.TgenFormatter(CompressionFormat.Binary);
        }
        /// <summary>
        /// Uses 'TgenSerializer' as a default Formatter
        /// </summary>
        public ServerManager(IPAddress localaddr, int port)
        {
            localEP = new IPEndPoint(localaddr, port);
            dualMode = false;

            active = false;
            formatter = new TgenSerializer.TgenFormatter(CompressionFormat.Binary);
        }
        /// <summary>
        /// Uses 'TgenSerializer' as a default Formatter
        /// </summary>
        public ServerManager(IPEndPoint localEP)
        {
            this.localEP = localEP;
            dualMode = false;

            active = false;
            formatter = new TgenSerializer.TgenFormatter(CompressionFormat.Binary);
        }

        public ServerManager(int port, IFormatter formatter)
        {
            localEP = new IPEndPoint(IPAddress.IPv6Any, port);
            dualMode = true;

            active = false;
            this.formatter = formatter;
        }
        public ServerManager(IPAddress localaddr, int port, IFormatter formatter)
        {
            localEP = new IPEndPoint(localaddr, port);
            dualMode = false;

            active = false;
            this.formatter = formatter;
        }
        public ServerManager(IPEndPoint localEP, IFormatter formatter)
        {
            this.localEP = localEP;
            dualMode = false;

            active = false;
            this.formatter = formatter;
        }

        private bool active; // field
        public bool Active   // property
        {
            get { return active; }
        }
        public int AmountOfClients   // property
        {
            get { return clients.Count; }
        }

        public string PublicIp
        {
            get { try { return new WebClient().DownloadString("http://icanhazip.com"); } catch(Exception) { return "Unable to load public IP"; } }
        }

        public int Port
        { get { return localEP.Port; } }
        public IPAddress Address
        { get { return localEP.Address; } }
        public AddressFamily AddressFamily
        { get { return localEP.AddressFamily; } }

        public const int PASSKEY_TIMEOUT = 5000;
        private byte[] passKey;
        public string PassKeyStr { 
            get => passKey != null ? Bytes.BytesToStr(passKey) : null; 
            set => passKey = value != null ? Bytes.StrToBytes(value) : null;
        }
        /// <summary> Key to accept incoming connections, can be set as string by `PassKeyStr` property.
        /// If set to null, every connection will be accepted </summary>
        public byte[] PassKey { get => passKey; set => passKey = value; }

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
                //throw new Exception("No network adapters with an IPv4 address in the system!");
            }
        }
        private Socket getNewListenSocket
        {
            get {
                Socket socket = new Socket(localEP.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.DualMode = dualMode;
                socket.Bind(localEP);
                return socket;
            }
        }

        private int clientsCount = 0; //an Id counter
        private ClientInfo AcceptIncomingClient()
        {
            TgenLog.Log("Accepting new socket!");
            Socket newClientListener = listener.Accept();

            newClientListener.NoDelay = true; //disables delay which occures when sending small chunks of data

            ClientInfo client = new ClientInfo(newClientListener, clientsCount);

            Task<bool> passCheck = CheckPass(client);
            checkPassList.Add((passCheck, client));

            return client;
        }

        private void AddApprovedClients((Task<bool> passCheck, ClientInfo clientInfo) data)
        {
            Task<bool> check = data.passCheck;
            if (!check.IsCompleted)
                return;

            checkPassList.Remove(data);

            ClientInfo info = data.clientInfo;
            bool approved = check.Result;
            if(!approved)
            {
                ClientDeclineEvent?.Invoke(info);
                info.client.Socket.Send(new byte[] { 0 /*FAILED*/});
                info.client.Close();
                return;
            }

            info.client.Socket.Send(new byte[] { 200 /*200 OK*/});

            clientsCount++;
            clients.Add(data.clientInfo);

            ClientConnectedEvent?.Invoke(data.clientInfo);
        }

        List<(Task<bool> passCheck, ClientInfo client)> checkPassList = new List<(Task<bool>, ClientInfo)>();
        public async Task<bool> CheckPass(ClientInfo info)
        {
            if (passKey == null)
                return true;

            Socket s = info.client.Socket;

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

            ClientPendingEvent?.Invoke(info, seg.Array, ref result); //`result` can be changed by event
            return result;
        }

        private void HandleClientPacket(ClientInfo client)
        {
            if (client.client.IsControlled) return;
            NetworkStream stm = client;
            try
            {
                if (!stm.DataAvailable) return;
                object message = Formatter.Deserialize(stm);
                TypeSetter.SendNewServerMessage(message, client);
            }
            //the program WILL crash when client hangs the server
            //the catch makes sure to handle the program properly when a client leaves
            catch
            {
                TgenLog.Log(client.id + " has disconnected");
                DropClient(client);
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
                ClientInfo client = clients[i];
                if (client) HandleClientPacket(client);
                else { DropClient(client); }
            }
        }


        private void ManageServer(int millisecondsTimeOutPerPoll, CancellationToken token)
        {
            while (active && !token.IsCancellationRequested)
            {
                //Make this function an automatic seperated thread poll events 
                //And take int milliseconds as an argument for a thread.sleep() to not overload the CPU
                PollEvents();
                Thread.Sleep(millisecondsTimeOutPerPoll);
            }
        }

        /// <summary>
        /// Is called to disconnect a client from the server (Close communications)
        /// </summary>
        /// <param name="client">The id of the client</param>
        private void AbortClient(ClientInfo client)
        {
            Socket socket = client;
            clients.Remove(client);
            socket.Close();
            ClientDisconnectedEvent?.Invoke(client);
        }

        /// <summary>
        /// The Server uses this function to drop inactive clients
        /// (Clients that disconnected/had a socket error)
        /// </summary>
        /// <param name="client"></param>
        private void DropClient(ClientInfo client)
        {
            TgenLog.Log("Aborting client: " + client);
            AbortClient(client);
        }

        /// <summary>
        /// Stop and drop communications with a client
        /// </summary>
        /// <param name="client"></param>
        public void KickClient(ClientInfo client)
        {
            if (client)
            {
                AbortClient(client);
                TgenLog.Log("Kicking client: " + client);
            }
            else
                TgenLog.Log("The client isn't active!");
        }

        /// <summary>
        /// Sends a message to a specific client based on the client's ID
        /// </summary>
        /// <param name="Message">The message you want to send</param>
        /// <param name="client">The id of the client who's supposed to get the message</param>
        /// <param name="throwOnError">Throw exception on failed send</param>
        public void Send(object Message, ClientInfo client, bool throwOnError = false)
        {
            if (client)
            {
                try
                {
                    NetworkStream stm = client;
                    Formatter.Serialize(stm, Message);
                }
                catch (Exception e) { if (throwOnError) throw e; /*client left as the message was serialized*/ }
            }
            else
                TgenLog.Log("You are trying to send a message to a client who's not connected!"); //Not really an error
                                                                                                  //throw new Exception("You are trying to send a message to a client who's not connected!");
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
                    ClientInfo client = clients[i];
                    Send(Message, client, throwOnError);
                }
            }
            else
                TgenLog.Log("No clients are connected!");
        }

        /// <summary>
        /// Sends a message to all connected clients except for the client,
        /// common feature among chats
        /// </summary>
        /// <param name="Message">The message</param>
        /// <param name="client">The client that won't get the message</param>
        /// /// <param name="throwOnError">Throw exception on failed send</param>
        public void SendToAllExcept(object Message, ClientInfo client, bool throwOnError = false)
        {
            if (clients.Count >= 0)
            {
                for (int i = 0; i < AmountOfClients; i++)
                {
                    ClientInfo currentClient = clients[i];
                    if (currentClient.id != client.id)
                        Send(Message, currentClient, throwOnError);
                }
            }
            else
                TgenLog.Log("No clients are connected!");
        }

        public void Start() => 
            Start((int)SocketOptionName.MaxConnections);

        public void Start(int backlog)
        {
            if (!active)
            {
                active = true;
                listener = getNewListenSocket;
                listener.Listen(backlog);
            }
            else
                TgenLog.Log("The listener is already open!");
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
            pollEventsTask = new Task(() => ManageServer(millisecondsTimeOutPerPoll, cancellationToken.Token), cancellationToken.Token);
            pollEventsTask.Start();

            return cancellationToken;
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
            else
                TgenLog.Log("The listener is already closed!");
        }
        /// <summary>
        /// Stops the listener then aborts all the connected client before it aborts the thread that listens to incoming clients
        /// </summary>
        public void Close()
        {
            if (active && listener != null)
            {
                Stop();
                for (int i = 0; i < AmountOfClients; i++)
                {
                    ClientInfo client = clients[i];
                    DropClient(client);
                }
                    
            }
            else
                TgenLog.Log("The listener is already closed!");
        }

        /// <summary>
        /// Closes the server
        /// </summary>
        public void Dispose()
        {
            Close();
        }
    }
}
