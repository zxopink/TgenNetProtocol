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
        public ClientManager()
        {
            tcpClient = new TcpClient();
        }

        public void Connect(string ip, int port)
        {
            tcpClient.Connect(ip, port);
            MessageListener = new Thread(ListenToIncomingMessages);
            MessageListener.Start();
        }

        public void Close()
        {
            MessageListener.Abort();
            tcpClient.Close();
        }

        public void Send(object message)
        {
            if (tcpClient.Connected)
            {
                NetworkStream stm = tcpClient.GetStream();
                BinaryFormatter bi = new BinaryFormatter();
                bi.Serialize(stm, message);
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
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e);
            }
        }
    }
}
