using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using TgenSerializer;

namespace TgenNetProtocol
{
    public class ServerManager : IDisposable
    {
        private Thread clientListenerThread;

        public delegate void NetworkActivity(ClientData client);
        public event NetworkActivity ClientDisconnectedEvent;
        public event NetworkActivity ClientConnectedEvent;
        private List<ClientData> clients = new List<ClientData>();
        private TcpListener listener;
        private readonly int port;

        private Formatter formatter;
        public Formatter Formatter { get => formatter; }
        //private bool listen = false; //made to control the listening thread

        public ServerManager(int port) 
        {this.port = port; active = false; formatter = new Formatter(FormatCompression.Binary); }
        public ServerManager(int port, Formatter formatter) 
        { this.port = port; active = false; this.formatter = formatter; }

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
        { get { return port; } }

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

        private int clientsCount = 0; //an Id counter
        private ClientData AcceptIncomingClient()
        {
            TgenLog.Log("Accepting new socket!");
            TcpClient newClientListener = listener.AcceptTcpClient();

            newClientListener.NoDelay = true; //disables delay which occures when sending small chunks or data
            newClientListener.Client.NoDelay = true; //disables delay which occures when sending small chunks or data

            ClientData client = new ClientData(newClientListener, clientsCount);
            clientsCount++;
            clients.Add(client);

            ClientConnectedEvent?.Invoke(client);

            return client;
        }

        private void HandleClientPacket(ClientData client)
        {
            if (client.client.IsControlled) return;
            TcpClient clientTcp = client;
            NetworkStream stm = clientTcp.GetStream();
            if (!stm.DataAvailable) return;
            try
            {
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

        private void ManageServer()
        {
            while (active)
            {
                while (listener.Pending())
                    AcceptIncomingClient(); //Method also adds the client to the clients list

                //Amount of clients can change during tick
                //int currentClients = AmountOfClients; //this value holds the connected clients during the tick
                for (int i = 0; i < AmountOfClients; i++) //AmountOfClients = length of clients list
                {
                    ClientData client = clients[i];
                    if (client) HandleClientPacket(client);
                    else { DropClient(client); }
                }
            }
        }

        /// <summary>
        /// Is called to disconnect a client from the server (Close communications)
        /// </summary>
        /// <param name="client">The id of the client</param>
        private void AbortClient(ClientData client)
        {
            TcpClient tcpClient = client;
            clients.Remove(client);
            tcpClient.Close();
            ClientDisconnectedEvent?.Invoke(client);
        }

        /// <summary>
        /// The Server uses this function to drop inactive clients
        /// (Clients that disconnected/had a socket error)
        /// </summary>
        /// <param name="client"></param>
        private void DropClient(ClientData client)
        {
            TgenLog.Log("Aborting client: " + client);
            AbortClient(client);
        }

        /// <summary>
        /// Stop and drop communications with a client
        /// </summary>
        /// <param name="client"></param>
        public void KickClient(ClientData client)
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
        public void Send(object Message, ClientData client, bool throwOnError = false)
        {
            TcpClient clientTcp = client;
            if (client)
            {
                try
                {
                    NetworkStream stm = clientTcp.GetStream();
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
                    ClientData client = clients[i];
                    Send(Message, client, throwOnError);
                }
            }
            else
                TgenLog.Log("No clients are connected!");
        }

        [Obsolete]
        /// <summary>
        /// Sends a message to all connected clients except for the client,
        /// common feature among chats
        /// </summary>
        /// <param name="Message">The message</param>
        /// <param name="client">The client that won't get the message</param>
        /// /// <param name="throwOnError">Throw exception on failed send</param>
        public void SendToAllExcept(object Message, ClientData client, bool throwOnError = false)
        {
            if (clients.Count >= 0)
            {
                for (int i = 0; i < AmountOfClients; i++)
                {
                    ClientData currentClient = clients[i];
                    if (currentClient.id != client.id)
                        Send(Message, currentClient, throwOnError);
                }
            }
            else
                TgenLog.Log("No clients are connected!");
        }

        public void Start()
        {
            if (!active)
            {
                active = true;
                listener = TcpListener.Create(port);
                listener.Start();
                clientListenerThread = new Thread(ManageServer);
                clientListenerThread.Start();
            }
            else
                TgenLog.Log("The listener is already open!");
        }
        public void Start(int backlog)
        {
            if (!active)
            {
                active = true;
                listener = TcpListener.Create(port);
                listener.Start(backlog);
                clientListenerThread = new Thread(ManageServer);
                clientListenerThread.Start();
            }
            else
                TgenLog.Log("The listener is already open!");
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
                listener.Stop();
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
                    ClientData client = clients[i];
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
