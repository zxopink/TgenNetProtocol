using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public abstract class NetworkBehavour : INetworkObject
    {
        public List<MethodData> ServerMethods { get; private set; }
        public List<MethodData> ClientMethods { get; private set; }
        public List<MethodData> DgramMethods { get; private set; }

        public NetworkBehavour()
        {
            SetUpMethods();

            //`System.Threading.Monitor` check later, responsible for thread work
            //Task.Run(AddToAttributes);
            Add2Attributes();
        }

        public void SetUpMethods()
        {
            MethodInfo[] methods = GetType().GetMethods();
            IEnumerable<MethodInfo> serverActions = methods.Where(x => x.GetCustomAttributes(typeof(ServerReceiverAttribute), false).FirstOrDefault() != null);
            IEnumerable<MethodInfo> clientActions = methods.Where(x => x.GetCustomAttributes(typeof(ClientReceiverAttribute), false).FirstOrDefault() != null);
            IEnumerable<MethodInfo> dgramAction = methods.Where(x => x.GetCustomAttributes(typeof(DgramReceiverAttribute), false).FirstOrDefault() != null);

            ServerMethods = GetMethodsData(serverActions);
            ClientMethods = GetMethodsData(clientActions);
            DgramMethods = GetMethodsData(dgramAction);
        }

        private List<MethodData> GetMethodsData(IEnumerable<MethodInfo> methods)
        {
            List<MethodData> methodsData = new List<MethodData>();
            foreach (MethodInfo item in methods)
                methodsData.Add(new MethodData(item, this));

            return methodsData;
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
        private void Add2Attributes()
        {
            TgenLog.Log("adding " + this.ToString() + " to the list");
            TypeSetter.networkObjects.Add(this);
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
                DgramMethods.Clear();
            }
        }

        private void Remove2Attributes()
        {
            int index = TypeSetter.networkObjects.IndexOf(this);
            if(index != -1) //Found
                TypeSetter.networkObjects[index] = null;

            ServerMethods.Clear();
            ClientMethods.Clear();
            DgramMethods.Clear();
        }

        public void Dispose() =>
            Remove2Attributes();//Task.Run(RemoveFromAttributes);

        public void InvokeNetworkMethods(MethodData method, object[] objetsToSend) =>
            method.Invoke(objetsToSend);
    }
}
