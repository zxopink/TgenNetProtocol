using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.CompilerServices;
using System.Linq;

namespace TgenNetProtocol
{
    struct ClientData
    {
        public TcpClient clientTcp;
        public int id;
    }
    public class ServerManager
    {
        private Thread clientListenerThread;
        public delegate void MessageSent(object message);
        public delegate void ClientDisconnected(int client);
        public delegate void ClientConnected(int client);
        public event ClientDisconnected ClientDisconnectedEvent;
        public event ClientConnected ClientConnectedEvent;
        private List<ClientData> clients = new List<ClientData>();
        private List<MessageSent> TypesSentEvents = new List<MessageSent>();
        private TcpListener listener;
        private List<TcpClient> tcpClientsList = new List<TcpClient>();
        private List<Thread> threadList = new List<Thread>();
        public ServerManager(int port) => listener = TcpListener.Create(port);

        private void HandleIncomingClients()
        {
            while (true)
            {
                if (listener.Pending())
                {
                    Console.WriteLine("ah");
                    Console.WriteLine("Accepting new socket!");
                    TcpClient newClientListener = listener.AcceptTcpClient();
                    ClientData client = new ClientData();
                    client.clientTcp = newClientListener;
                    client.id = tcpClientsList.Count;
                    clients.Add(client);
                    ClientConnectedEvent?.Invoke(tcpClientsList.Count);
                    tcpClientsList.Add(newClientListener);

                    Thread t = new Thread(HandleIncomingClinetMessages);
                    threadList.Add(t);
                    //t.Start(sList[sList.Count - 1]);
                    t.Start(client);
                }
            }
        }

        private void HandleIncomingClinetMessages(Object newC)
        {
            ClientData clientData = (ClientData)newC;
            TcpClient clientTcp = clientData.clientTcp;
            NetworkStream stm = clientTcp.GetStream();
            try
            {
                while (clientTcp.Connected)
                {
                    BinaryFormatter bi = new BinaryFormatter();
                    object message = bi.Deserialize(stm);
                    //ServerCommunication.Program.MessageRecived(message, user);
                    //ServerNetworkReciverAttribute callAll = new ServerNetworkReciverAttribute();
                    //callAll.SendNewMessage(message);
                    AttributeActions.SendNewServerMessage(message, clientData.id);
                }
            }
            //the program WILL crash when client server
            //the catch makes sure to handle the program properly when a client leaves
            catch (Exception e)
            {
                //Console.WriteLine("Error: " + e);
                //Console.WriteLine(clientData.id + " has disconnected");
                stm.Close();
                stm.Dispose();
                AbortClient(clientData.id);
            }
        }

        /// <summary>
        /// Is called as an exeption for when a client leaves
        /// </summary>
        /// <param name="client">The id of the client</param>
        private void AbortClient(int client)
        {
            ClientDisconnectedEvent?.Invoke(client);
            Thread deadClientThread = threadList[client];
            //MessageSentEvent -= (ServerCommunication.MessageSent)MessageBackMethod;
            //it doesn't remove from the list because removing from the list makes the capacity smaller
            //so clients who their id is bigger than the capacity will crash the program
            Console.WriteLine("Aborting client: " + client);
            tcpClientsList[client] = null;
            threadList[client] = null;
            if (deadClientThread != null)
                deadClientThread.Abort();
        }

        /// <summary>
        /// useless right now
        /// </summary>
        /// <param name="client"></param>
        protected virtual void ClientAborted(int client)
        {
            Console.WriteLine("inside class call");
        }

        public void KickClient(int client)
        {
            if (client < clients.Count)
            {
                Thread deadClientThread = threadList[client];
                Console.WriteLine("Kicking client: " + client);
                tcpClientsList[client] = null;
                threadList[client] = null;
                deadClientThread.Abort();
            }
            else
                Console.WriteLine("The client you tried to kick isn't in the list!");
        }

        /// <summary>
        /// Sends a message to a specific client based on the client's ID
        /// </summary>
        /// <param name="Message">The message you want to send</param>
        /// <param name="client">The id of the client who's supposed to get the message</param>
        public void Send(object Message, int client)
        {
            TcpClient clientTcp = clients[client].clientTcp;
            if (clientTcp.Connected)
            {
                NetworkStream stm = clientTcp.GetStream();
                BinaryFormatter bi = new BinaryFormatter();
                bi.Serialize(stm, Message);
            }
            else
                Console.WriteLine("You are trying to send a message to a client who's not connected!");
        }

        /// <summary>
        /// Sends a message to all connected client,
        /// common feature among chats
        /// </summary>
        /// <param name="Message">The message you want to send</param>
        public void SendToAll(object Message)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                TcpClient clientTcp = clients[i].clientTcp;
                if (clientTcp.Connected)
                {
                    NetworkStream stm = clientTcp.GetStream();
                    BinaryFormatter bi = new BinaryFormatter();
                    bi.Serialize(stm, Message);
                }
            }
        }

        /// <summary>
        /// Sends a message to all connected clients except for the client
        /// </summary>
        /// <param name="Message">The message</param>
        /// <param name="client">The client that won't get the message</param>
        public void SendToAllExcept(object Message, int client)
        {
            for (int i = 0; i < clients.Count; i++)
            {
                if (i != client)
                {
                    TcpClient clientTcp = clients[i].clientTcp;
                    if (clientTcp.Connected)
                    {
                        NetworkStream stm = clientTcp.GetStream();
                        BinaryFormatter bi = new BinaryFormatter();
                        bi.Serialize(stm, Message);
                    }
                }
            }
        }

        public void Start()
        {
            listener.Start();
            clientListenerThread = new Thread(HandleIncomingClients);
            clientListenerThread.Start();
        }
        public void Start(int backlog)
        {
            listener.Start(backlog);
            clientListenerThread = new Thread(HandleIncomingClients);
            clientListenerThread.Start();
        }

        /// <summary>
        /// Stops the listener then aborts all the connected client before it aborts the  thread that listens to incoming clients
        /// </summary>
        public void Stop()
        {
            for (int i = 0; i < tcpClientsList.Count; i++)
                AbortClient(i);
            clientListenerThread.Abort();
            listener.Stop();
        }
    }
}
