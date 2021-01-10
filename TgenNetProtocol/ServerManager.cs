using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Runtime.InteropServices;
using System.Net;
using TgenSerializer;
using System.IO;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public class NetWorkEvents : EventArgs
    {
        public int Client = -1;
        public object message;
    }
    public class ClientData
    {
        public Thread clientReceiveThread;
        public TcpClient clientTcp;
        public bool activeClient;
        [Obsolete]
        public int id;

        public override string ToString() => id.ToString();
    }
    public class ServerManager : IDisposable
    {
        private Thread clientListenerThread;
        public delegate void MessageSent(object message);
        //public delegate void ClientDisconnected(int client);
        //public delegate void ClientConnected(int client);
        public delegate void NetworkActivity(ClientData client);
        public event NetworkActivity ClientDisconnectedEvent;
        public event NetworkActivity ClientConnectedEvent;
        private List<ClientData> clients = new List<ClientData>();
        //private List<MessageSent> TypesSentEvents = new List<MessageSent>();
        private TcpListener listener;
        //private List<TcpClient> tcpClientsList = new List<TcpClient>();
        //private List<Thread> threadList = new List<Thread>();
        private int port;

        //private bool listen = false; //made to control the listening thread

        public ServerManager(int port) {this.port = port; AttributeActions.CheckAvailableAssemblies(); }

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

        private void HandleIncomingClients()
        {
            while (active)
            {
                try
                {
                    if (active && listener.Pending())
                    {
                        TgenLog.Log("Accepting new socket!");
                        TcpClient newClientListener = listener.AcceptTcpClient();

                        newClientListener.NoDelay = true; //disables delay which occures when sending small chunks or data
                        newClientListener.Client.NoDelay = true; //disables delay which occures when sending small chunks or data

                        ClientData client = new ClientData();
                        client.clientTcp = newClientListener;
                        client.activeClient = true;
                        client.id = clients.Count;

                        //CheckForStream(client);

                        Thread t = new Thread(HandleIncomingClinetMessages);
                        client.clientReceiveThread = t;
                        //threadList.Add(t);
                        //t.Start(sList[sList.Count - 1]);
                        t.Start(client); //let the client start before you add him to the list
                        clients.Add(client);
                        //tcpClientsList.Add(newClientListener);
                        ClientConnectedEvent?.Invoke(client);
                    }
                }
                catch (SocketException)
                {
                    TgenLog.Log("Had issue accepting an incoming client");
                }
            }
        }

        [Obsolete] //Unused for now
        private void AddClientToList(List<ClientData> clients, ClientData client)
        {
            bool added = false;
            for (int i = 0; i < clients.Count; i++)
            {
                ClientData clientSpot = clients[i];
                if (clientSpot == null)
                {
                    clients[i] = client;
                    added = true;
                    break;
                }
            }

            if (!added)
            {
                clients.Add(client);
            }
        }
        [Obsolete] //Unused for now
        private void CleanClientsList(List<ClientData> clients) => clients.RemoveAll(item => item == null);

        /// <summary>
        /// This method makes sure the server and client can talk
        /// through the network stream
        /// </summary>
        /// <param name="newC"></param>
        private void CheckForStream(ClientData newC)
        {
            ClientData clientData = newC;
            TcpClient clientTcp = clientData.clientTcp;
            NetworkStream stm = clientTcp.GetStream();
            Console.WriteLine(clientTcp.Client.LocalEndPoint.AddressFamily);
            stm.ReadTimeout = 100; //sets a readtimeout so the thread which listens to client won't hold for too long
            try
            {
                BinaryFormatter bi = new BinaryFormatter();
                object message = bi.Deserialize(stm);
                if (message.ToString() == "Connected")
                    bi.Serialize(stm, message);
                else
                    throw new SocketException();
                stm.ReadTimeout = Timeout.Infinite; //sets back the readtimeout to infinite
            }
            catch (Exception)
            {
                stm.Close(); //close is disposed
                clientTcp.Close();
                throw new SocketException();
            }
        }

        private void ClientsMessageReceiverManager()
        {
            foreach (var client in clients)
            {
                if (!client.activeClient)
                    continue; //Not active? Continue

                TcpClient tcpClient = client.clientTcp;

                if (tcpClient.Connected && tcpClient.Available > 0)
                { 
                //FINISH
                }
            }
        }

        private void HandleIncomingClinetMessages(Object newC)
        {
            ClientData clientData = (ClientData)newC;
            TcpClient clientTcp = clientData.clientTcp;
            NetworkStream stm = clientTcp.GetStream();
            //try
            //{
                while (clientTcp.Connected && clientData.activeClient)
                {
                    //Console.WriteLine("Got Message");
                    //BinaryFormatter bi = new BinaryFormatter();
                    //object message = bi.Deserialize(stm);

                    object message = TgenFormatter.Deserialize(stm);
                    //ServerCommunication.Program.MessageRecived(message, user);
                    //ServerNetworkReciverAttribute callAll = new ServerNetworkReciverAttribute();
                    //callAll.SendNewMessage(message);
                    AttributeActions.SendNewServerMessage(message, clientData.id);
                }
            //}
            //the program WILL crash when client server
            //the catch makes sure to handle the program properly when a client leaves
            //catch (Exception e)
            //{
            //    TgenLog.Log(e.ToString());
                //Console.WriteLine("Error: " + e);
                //Console.WriteLine(clientData.id + " has disconnected");
            //}
            stm.Close(); //close is disposed
            DropClient(clientData);
        }

        [Obsolete]
        /// <summary>
        /// Is called to disconnect a client from the server (Close communications)
        /// </summary>
        /// <param name="client">The id of the client</param>
        private void AbortClient(int client)
        {
            ClientData data = clients[client];
            Thread deadClientThread = data.clientReceiveThread;
            TcpClient tcpClient = data.clientTcp;
            if (deadClientThread != null && tcpClient != null)
            {
                ClientDisconnectedEvent?.Invoke(data);
                //MessageSentEvent -= (ServerCommunication.MessageSent)MessageBackMethod;
                //it doesn't remove from the list because removing from the list makes the capacity smaller
                //so clients who their id is bigger than the capacity will crash the program
                clients[client] = null;
                tcpClient.Close();
                data.activeClient = false;
            }
        }

        private void AbortClient(ClientData client)
        {
            if (client == null)
                return;

            Thread deadClientThread = client.clientReceiveThread;
            TcpClient tcpClient = client.clientTcp;
            if (deadClientThread != null && tcpClient != null)
            {
                ClientDisconnectedEvent?.Invoke(client);
                //MessageSentEvent -= (ServerCommunication.MessageSent)MessageBackMethod;
                //it doesn't remove from the list because removing from the list makes the capacity smaller
                //so clients who their id is bigger than the capacity will crash the program
                clients[client.id] = null;
                //clients.Remove(client);
                tcpClient.Close();
                client.activeClient = false;
            }
        }

        [Obsolete]
        /// <summary>
        /// The Server uses this function to drop inactive clients
        /// (Clients that disconnected/had a socket error)
        /// </summary>
        /// <param name="client"></param>
        private void DropClient(int client)
        {
            TgenLog.Log("Aborting client: " + client);
            AbortClient(client);
        }
        private void DropClient(ClientData client)
        {
            TgenLog.Log("Aborting client: " + client);
            AbortClient(client);
        }

        [Obsolete]
        /// <summary>
        /// Stop and drop communications with a client
        /// </summary>
        /// <param name="client"></param>
        public void KickClient(int client)
        {
            ClientData data = clients[client];
            if (client < clients.Count)
            {
                AbortClient(client);
                TgenLog.Log("Kicking client: " + client);

            }
            else
                TgenLog.Log("The client you tried to kick isn't in the list!");
        }
        public void KickClient(ClientData client)
        {
            if (client.activeClient)
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
        public void Send(object Message, int client = 0, bool throwOnError = false)
        {
            ClientData data = clients[client];
            TcpClient clientTcp = data.clientTcp;
            Thread clientThread = data.clientReceiveThread;
            if (clientTcp.Connected && clientThread != null)
            {
                try
                {
                    NetworkStream stm = clientTcp.GetStream();
                    //BinaryFormatter bi = new BinaryFormatter();
                    //bi.Serialize(stm, Message);
                    TgenFormatter.Serialize(stm, Message);
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
        public void SendToAll(object Message, bool throwOnError = false)
        {
            if (clients.Count >= 0)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (clients[i].clientTcp.Connected)
                        Send(Message, i, throwOnError);
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
        public void SendToAllExcept(object Message, int client, bool throwOnError = false)
        {
            if (clients.Count >= 0)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (i != client)
                    {
                        if (clients[i].clientTcp.Connected)
                            Send(Message, i, throwOnError);
                    }
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
                clientListenerThread = new Thread(HandleIncomingClients);
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
                clientListenerThread = new Thread(HandleIncomingClients);
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
                foreach (var client in clients)
                    DropClient(client);
            }
            else
                TgenLog.Log("The listener is already closed!");
        }

        public void Dispose()
        {
            Close();
        }
    }
}
