using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Reflection;

namespace TgenNetProtocol
{
    public struct MethodInvokeData
    {
        public MethodInfo method;
        public object methodObjects;
        public object[] parameters;
    }
    public abstract class NetworkBehavour : INetworkObject
    {
        public List<MethodInvokeData> methods = new List<MethodInvokeData>();
        private Thread myThread;

        private IEnumerable<MethodInfo> serverMethods;
        private IEnumerable<MethodInfo> clientMethods;
        public IEnumerable<MethodInfo> ServerMethods { get => serverMethods; }
        public IEnumerable<MethodInfo> ClientMethods { get => clientMethods; }

        public delegate void NetworkMethod();
        /// <summary>
        /// What's for?
        /// </summary>
        [Obsolete] //finish with current changes before you mess with this
        public static event NetworkMethod NetworkMethodEvent;
        public NetworkBehavour()
        {
            SetUpMethods();

            NetworkMethodEvent += InvokeMethods;
            myThread = Thread.CurrentThread; //main thread
            Thread addToList = new Thread(AddToAttributes);
            addToList.Start();
        }

        public void SetUpMethods()
        {
            Type type = this.GetType();
            serverMethods = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ServerNetworkReciverAttribute), false).FirstOrDefault() != null);
            clientMethods = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ClientNetworkReciverAttribute), false).FirstOrDefault() != null);
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
            }
        }

        public void Dispose()
        {
            NetworkMethodEvent -= InvokeMethods;

            Thread removeFromList = new Thread(RemoveFromAttributes);
            removeFromList.Start();
        }

        public static void RunMethods()
        {
            NetworkMethodEvent();
        }

        public void InvokeMethods()
        {
            if (Thread.CurrentThread == myThread)
            {
                TgenLog.Log("main thread is invoking");
                foreach (var methodData in methods)
                {
                    methodData.method.Invoke(methodData.methodObjects, methodData.parameters);
                }
            }
            else
            {
                TgenLog.Log("That's weird, a different thread has tried to invoke");
            }
        }

        public void InvokeNetworkMethods(MethodInfo method, object[] objetsToSend, object ObjectThatOwnsTheMethod) =>
            method.Invoke(ObjectThatOwnsTheMethod, objetsToSend);
    }
}
