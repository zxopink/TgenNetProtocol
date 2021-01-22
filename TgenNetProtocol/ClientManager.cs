using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using TgenSerializer;

namespace TgenNetProtocol
{
    public class ClientManager : IDisposable
    {
        private TcpClient tcpClient;
        private Thread MessageListener;

        //public event EventHandler OnConnect;
        public delegate void ClientActivity();
        public event ClientActivity OnConnect;
        /// <summary>
        /// On connection aborted
        /// </summary>
        public event ClientActivity OnDisconnect;

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
                //throw new Exception("No network adapters with an IPv4 address in the system!");
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
            if (active)
            {
                TgenLog.Log("Client is already connected to a server!");
                return true;
            }

            try
            {
                tcpClient = new TcpClient(); //makes a new TcpClient in case the client wants to use the same clientmanager and reuse it or reconnect
                tcpClient.Connect(ip, port);
                //CheckForStream();

                tcpClient.NoDelay = true; //disables delay which occures when sending small chunks or data
                tcpClient.Client.NoDelay = true; //disables delay which occures when sending small chunks or data

                MessageListener = new Thread(ListenToIncomingMessages);
                MessageListener.Start();
                attemptCounter = 0;
                active = true;
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
                    //throw new Exception("Was not able to connect the server after " + maxAttemptCount + " attempts"); //a console print is enough
                }
                else
                    Connect(ip, port);
            }
            return false;
        }

        public async Task<bool> ConnectAsync(string ip, int port)
        {
            if (active)
            {
                TgenLog.Log("Client is already connected to a server!");
                return true;
            }
            try
            {
                tcpClient = new TcpClient(); //makes a new TcpClient in case the client wants to use the same clientmanager and reuse it or reconnect
                Task connectOperation = tcpClient.ConnectAsync(ip, port).ContinueWith((x) =>
                {
                    if (x.IsFaulted)
                        throw x.Exception.InnerException;

                        //CheckForStream();
                        MessageListener = new Thread(ListenToIncomingMessages);
                    MessageListener.Start();
                    attemptCounter = 0;
                    OnConnect?.Invoke();
                    active = true;
                });
                tcpClient.NoDelay = true; //disables delay which occures when sending small chunks or data
                tcpClient.Client.NoDelay = true; //disables delay which occures when sending small chunks or data
                await connectOperation; //wait for the connection part to finish

                if (!connectOperation.IsFaulted) //was an exception thrown?
                    throw connectOperation.Exception.InnerException; //if so, throw the exception to go through the catch section

                return true; //could use connectOperation.IsCompleted but if it completed then the connection must be true
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
                    await ConnectAsync(ip, port);
            }
            return false;
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
                stm.Close(); //close is like dispose
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
                    //BinaryFormatter bi = new BinaryFormatter();
                    //bi.Serialize(stm, message);
                    TgenFormatter.Serialize(stm, message);
                }
                else
                    Console.WriteLine("The client isn't connected to a server!");
            }
            catch (Exception e) //Usually gets thrown when the server aborted/kicked the client
            {
                Close();
                TgenLog.Log(e.ToString());
                throw e;
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
                        //BinaryFormatter bi = new BinaryFormatter();
                        //object message = bi.Deserialize(stm);

                        object message = TgenFormatter.Deserialize(stm);
                        //ClientNetworkReciverAttribute network = new ClientNetworkReciverAttribute();
                        //network.SendNewMessage(Message);
                        AttributeActions.SendNewClientMessage(message);
                    }
                }
                Close();
                OnDisconnect?.Invoke();
            }
            catch (ThreadAbortException e)
            {
                //this usually happens when the client closes the listener
                //since the thread isn't in use so it aborts it
                Close();
                //TgenLog.Log(e.ToString());
                OnDisconnect?.Invoke();
                //throw e;
            }

            catch (ObjectDisposedException e)
            {
                //this usually happens when the client closes the ClientTcp
                //the ClientTcp is disposed(null) so it can't get the connected property of it
                Close();
                //TgenLog.Log(e.ToString());
                OnDisconnect?.Invoke();
                //throw e;
            }

            catch (Exception e)
            {
                //TgenLog.Log(e.ToString());
                Close();
                OnDisconnect?.Invoke();
                //throw e;
            }
        }

        public void Dispose()
        {
            Close();
            //GC.SuppressFinalize(this);
        }
    }
}
