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

        public void Connect(string ip, int port)
        {
            tcpClient = new TcpClient(); //makes a new TcpClient in case the client wants to use the same clientmanager and reuse it or reconnect
            tcpClient.Connect(ip, port);
            MessageListener = new Thread(ListenToIncomingMessages);
            MessageListener.Start();
        }

        public void Close()
        {
            tcpClient.Close();
            MessageListener.Abort();
        }

        public void Send(object message)
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
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.GetType());
            }
        }
    }
}
