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
        //public bool isConnected { get => tcpClient.Connected; }
        public ClientManager()
        {
            tcpClient = new TcpClient(); //make an empty one that will be replaced for later
        }

        public bool IsConnected()
        {
            return tcpClient.Connected;
        }

        int attemptCounter = 0;
        int maxAttemptCount = 4;
        public void Connect(string ip, int port)
        {
            try
            {
                tcpClient = new TcpClient(); //makes a new TcpClient in case the client wants to use the same clientmanager and reuse it or reconnect
                tcpClient.Connect(ip, port);

                CheckForStream();

                MessageListener = new Thread(ListenToIncomingMessages);
                MessageListener.Start();
                attemptCounter = 0;
            }
            catch (SocketException)
            {
                attemptCounter++;
                Console.WriteLine("Attempt number " + attemptCounter + " to connect the server");
                if (attemptCounter == maxAttemptCount)
                {
                    throw new Exception("Was not able to connect the server after " + maxAttemptCount + " attempts");
                }
                else
                    Connect(ip, port);
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
            MessageListener.Abort();
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
                while (tcpClient.Connected)
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
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.GetType());
                Close();
            }
        }
    }
}
