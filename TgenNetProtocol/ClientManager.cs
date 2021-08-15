using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using TgenSerializer;

namespace TgenNetProtocol
{
    public class ClientManager : IDisposable
    {
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
            client = (Client)new TcpClient(); //make an empty one that will be replaced for later
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
        public bool Connect(string ip, int port)
        {
            if (client)
            {
                TgenLog.Log("Client is already connected to a server!");
                return true;
            }

            try
            {
                client.Connect(ip, port);

                MessageListener = new Thread(ListenToIncomingMessages);
                MessageListener.Start();
                attemptCounter = 0;
                OnConnect?.Invoke();
                return true;
            }
            catch (SocketException e)
            {
                TgenLog.Log(e.ToString());

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
                    Connect(ip, port);
            }
            return false;
        }

        public void Close()
        {
            client.Close();
            client = (Client)new TcpClient();
        }

        public void Send(object message, bool throwOnError = false)
        {
            try
            {
                if (client)
                {
                    NetworkStream stm = client;
                    TgenFormatter.Serialize(stm, message);
                }
                else
                    Console.WriteLine("The client isn't connected to a server!");
            }
            catch (Exception e) //Usually gets thrown when the server aborted/kicked the client
            {
                Close();
                TgenLog.Log(e.ToString());
                if (throwOnError)
                    throw e;
            }
        }

        private void ListenToIncomingMessages()
        {
            try
            {
                NetworkStream stm = client;
                while (client)
                {
                    if (stm.DataAvailable && !client.IsControlled)
                    {
                        object message = TgenFormatter.Deserialize(stm);
                        TypeSetter.SendNewClientMessage(message);
                    }
                }
                OnDisconnect?.Invoke();
            }
            catch (ThreadAbortException e)
            {
                //this usually happens when the client closes the listener
                //since the thread isn't in use so it aborts it
                OnDisconnect?.Invoke();
            }

            catch (ObjectDisposedException e)
            {
                //this usually happens when the client closes the ClientTcp
                //the ClientTcp is disposed(null) so it can't get the connected property of it
                OnDisconnect?.Invoke();
            }

            catch (Exception e)
            {
                OnDisconnect?.Invoke();
            }
        }

        public void Dispose()
        {
            Close();
        }
    }
}
