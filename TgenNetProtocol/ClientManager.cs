using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace TgenNetProtocol
{
    public class ClientManager
    {
        private TcpClient tcpClient;
        private Thread MessageListener;

        public event EventHandler OnConnect;

        private bool active; // field
        /// <summary>
        /// Checks if the listener for messages is active
        /// </summary>
        public bool Active   // property
        {
            get { return active; }
        }

        public string PublicIp
        {
            get { return new WebClient().DownloadString("http://icanhazip.com"); }
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
                throw new Exception("No network adapters with an IPv4 address in the system!");
            }
        }

        //public bool isConnected { get => tcpClient.Connected; }
        public ClientManager()
        {
            tcpClient = new TcpClient(); //make an empty one that will be replaced for later
            active = false;
        }

        public bool IsConnected()
        {
            return tcpClient.Connected;
        }

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
        public bool Connect(string ip, int port)
        {
            if (!active)
            {
                try
                {
                    tcpClient = new TcpClient(); //makes a new TcpClient in case the client wants to use the same clientmanager and reuse it or reconnect
                    tcpClient.Connect(ip, port);
                    CheckForStream();

                    tcpClient.NoDelay = true; //disables delay which occures when sending small chunks or data
                    tcpClient.Client.NoDelay = true; //disables delay which occures when sending small chunks or data

                    MessageListener = new Thread(ListenToIncomingMessages);
                    MessageListener.Start();
                    attemptCounter = 0;
                    active = true;
                    return true;
                }
                catch (SocketException)
                {
                    if (!makeAttempts)
                        return false;

                    attemptCounter++;
                    Console.WriteLine("Attempt number " + attemptCounter + " to connect the server");
                    if (attemptCounter == maxAttemptCount)
                    {
                        attemptCounter = 0;
                        Console.WriteLine("Was not able to connect the server after " + maxAttemptCount + " attempts");
                        return false;
                        //throw new Exception("Was not able to connect the server after " + maxAttemptCount + " attempts"); //a console print is enough
                    }
                    else
                        Connect(ip, port);
                }
                return false;
            }
            else
            {
                Console.WriteLine("Client is already connected to a server!");
                return true;
            }
        }

        private void CheckForStream()
        {
            NetworkStream stm = tcpClient.GetStream();
            stm.ReadTimeout = 100;
            try
            {
                BinaryFormatter bi = new BinaryFormatter();
                bi.Serialize(stm, "Connected");
                object message = bi.Deserialize(stm);
                if (message.ToString() != "Connected")
                    throw new SocketException();
                stm.ReadTimeout = -1;
            }
            catch
            {
                stm.Close();
                stm.Dispose();
                tcpClient.Close();
                throw new SocketException();
            }
        }

        public void Close()
        {
            tcpClient.Close();
            active = false;
            //MessageListener.Abort();
        }

        public void Send(object message)
        {
            try
            {
                if (tcpClient.Connected)
                {
                    NetworkStream stm = tcpClient.GetStream();
                    BinaryFormatter bi = new BinaryFormatter();
                    bi.Serialize(stm, message);
                }
                else
                    Console.WriteLine("The client isn't connected to a server!");
            }
            catch //Usually gets thrown when the server aborted/kicked the client
            {
                Close();
            }
        }

        private void ListenToIncomingMessages()
        {
            try
            {
                NetworkStream stm = tcpClient.GetStream();
                while (active && tcpClient.Connected)
                {
                    if (stm.DataAvailable)
                    {
                        BinaryFormatter bi = new BinaryFormatter();
                        object message = bi.Deserialize(stm);
                        //ClientNetworkReciverAttribute network = new ClientNetworkReciverAttribute();
                        //network.SendNewMessage(Message);
                        AttributeActions.SendNewClientMessage(message);
                    }
                }
            }
            catch (ThreadAbortException)
            {
                //this usually happens when the client closes the listener
                //since the thread isn't in use so it aborts it
                Close();
            }

            catch (ObjectDisposedException)
            {
                //this usually happens when the client closes the ClientTcp
                //the ClientTcp is disposed(null) so it can't get the connected property of it
                Close();
            }

            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.GetType());
                Close();
            }
        }
    }
}
