using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;

namespace TgenNetProtocol
{
    public struct MethodInvokeData
    {
        public MethodInfo method;
        public object theMethodObject;
        public object[] parameters;
    }
    public class NetworkBehavour
    {
        public List<MethodInvokeData> methods = new List<MethodInvokeData>();
        private Thread myThread;
        public delegate void NetworkMethod();
        public static event NetworkMethod NetworkMethodEvent;
        public NetworkBehavour()
        {
            NetworkMethodEvent += InvokeMethods;
            myThread = Thread.CurrentThread;
            Thread addToList = new Thread(AddToAttributes);
            addToList.Start();
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
                if (!AttributeActions.isWorking)
                {
                    AttributeActions.networkObjects.Add(this);
                    isDone = true;
                }
            }
        }

        public static void RunMethods()
        {
            NetworkMethodEvent();
        }

        public void InvokeMethods()
        {
            if (Thread.CurrentThread == myThread)
            {
                foreach (var methodData in methods)
                {
                    methodData.method.Invoke(methodData.theMethodObject, methodData.parameters);
                }
            }
        }
    }
}
