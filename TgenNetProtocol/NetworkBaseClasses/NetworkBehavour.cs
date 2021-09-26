using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public abstract class NetworkBehavour : INetworkObject
    {
        private List<MethodData> serverMethods;
        private List<MethodData> clientMethods;
        public List<MethodData> ServerMethods { get => serverMethods; }
        public List<MethodData> ClientMethods { get => clientMethods; }

        public NetworkBehavour()
        {
            SetUpMethods();

            Task.Run(AddToAttributes);
        }

        public void SetUpMethods()
        {
            Type type = GetType();
            IEnumerable<MethodInfo> serverActions = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ServerReceiverAttribute), false).FirstOrDefault() != null);
            IEnumerable<MethodInfo> clientActions = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ClientReceiverAttribute), false).FirstOrDefault() != null);

            serverMethods = GetMethodsData(serverActions);
            clientMethods = GetMethodsData(clientActions);
        }

        private List<MethodData> GetMethodsData(IEnumerable<MethodInfo> methods)
        {
            List<MethodData> methodDatas = new List<MethodData>();
            foreach (MethodInfo item in methods)
                methodDatas.Add(new MethodData(item, this));

            return methodDatas;
        }

        /// <summary>
        /// This method makes sure the other threads that sends message isn't getting effected while it's active
        /// Things can break if two thread work on the same variable/method
        /// </summary>
        private void AddToAttributes()
        {
            bool isDone = false;
            while (!isDone)
            {
                if (!TypeSetter.isWorking)
                {
                    TgenLog.Log("adding " + this.ToString() + " to the list");
                    TypeSetter.networkObjects.Add(this);
                    isDone = true;
                }
            }
        }

        private void RemoveFromAttributes()
        {
            bool isDone = false;
            while (!isDone)
            {
                if (!TypeSetter.isWorking)
                {
                    TypeSetter.networkObjects.Remove(this);
                    isDone = true;
                }
                ServerMethods.Clear();
                ClientMethods.Clear();
            }
        }

        public void Dispose() =>
            Task.Run(RemoveFromAttributes);

        public void InvokeNetworkMethods(MethodData method, object[] objetsToSend) =>
            method.Invoke(objetsToSend);
    }
}
