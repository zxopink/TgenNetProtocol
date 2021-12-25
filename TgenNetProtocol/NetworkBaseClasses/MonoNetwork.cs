using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
//using UnityEngine;

namespace TgenNetProtocol
{
    public class SafeMonoInvokeData
    {
        public SafeMonoInvokeData(MethodInfo method, object theMethodObject, object[] parameters)
        {
            this.method = method;
            this.theMethodObject = theMethodObject;
            this.parameters = parameters;
        }
        public MethodInfo method;
        public object theMethodObject;
        public object[] parameters;
    }
    //#if UNITY_5_3_OR_NEWER
    public class MonoNetwork //: MonoBehavour, INetworkObject
    {
        //'volatile', might wanna check what that does
        volatile List<SafeMonoInvokeData> waitingMethods = new List<SafeMonoInvokeData>();
        volatile bool isInvokingMethods = false;

        public List<MethodData> ServerMethods { get; private set; }
        public List<MethodData> ClientMethods { get; private set; }
        public List<MethodData> DgramMethods { get; private set; }

        public MonoNetwork()
        {
            SetUpMethods();

            Thread addToList = new Thread(AddToAttributes);
            addToList.Start();
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
        /// Being called each frame, syncs the game engine with function's invoking order
        /// </summary>
        public void Update()
        {
            isInvokingMethods = true;
            lock (waitingMethods)
            {
                if (waitingMethods.Count != 0)
                {
                    foreach (var methodData in waitingMethods) //maybe change waitingMethods to waitingMethods.ToList() if problems raise
                    {
                        methodData.method.Invoke(methodData.theMethodObject, methodData.parameters);
                        //waitingMethods.Remove(methodData);
                    }
                    waitingMethods = new List<SafeMonoInvokeData>();
                }
            }
            isInvokingMethods = false;
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
                    //TypeSetter.networkObjects.Add(this);
                    isDone = true;
                }
            }
        }

        private void WaitToChangeInvokeList(object obj)
        {
            SafeMonoInvokeData data = (SafeMonoInvokeData)obj;
            bool isDone = false;
            while (!isDone)
            {
                lock (waitingMethods)
                {
                    if (!isInvokingMethods)
                    {
                        waitingMethods.Add(data);
                        isDone = true;
                    }
                }
            }
        }

        /// <summary>
        /// will not work on static methods
        /// </summary>
        /// <param name="method">The Method to invoke</param>
        /// <param name="objetsToSend">the arguments the Method takes</param>
        /// <param name="ObjectThatOwnsTheMethod">The object that 'owns' the method</param>
        public void InvokeNetworkMethods(MethodInfo method, object[] objetsToSend, object ObjectThatOwnsTheMethod)
        {
            if (!method.IsStatic)
            {
                var tArgs = new List<Type>();
                foreach (var param in method.GetParameters())
                    tArgs.Add(param.ParameterType);
                tArgs.Add(method.ReturnType);
                var delDecltype = Expression.GetDelegateType(tArgs.ToArray());
                var del = Delegate.CreateDelegate(delDecltype, ObjectThatOwnsTheMethod, method);

                //waitingMethods.Add(new SafeMonoInvokeData(method, ObjectThatOwnsTheMethod, objetsToSend));

                //WaitToChangeInvokeRemade(new SafeMonoInvokeData(method, ObjectThatOwnsTheMethod, objetsToSend));

                Thread addToList = new Thread(WaitToChangeInvokeList);
                addToList.Start(new SafeMonoInvokeData(method, ObjectThatOwnsTheMethod, objetsToSend));
            }
            else
            {
                var tArgs = new List<Type>();
                foreach (var param in method.GetParameters())
                    tArgs.Add(param.ParameterType);
                tArgs.Add(method.ReturnType);
                var delDecltype = Expression.GetDelegateType(tArgs.ToArray());
                var del = Delegate.CreateDelegate(delDecltype, method);

                //waitingMethods.Add(new SafeMonoInvokeData(method, ObjectThatOwnsTheMethod, objetsToSend));

                //WaitToChangeInvokeRemade(new SafeMonoInvokeData(method, ObjectThatOwnsTheMethod, objetsToSend));

                Thread addToList = new Thread(WaitToChangeInvokeList);
                addToList.Start(new SafeMonoInvokeData(method, ObjectThatOwnsTheMethod, objetsToSend));
            }
        }

        private void RemoveFromAttributes()
        {
            bool isDone = false;
            while (!isDone)
            {
                if (!TypeSetter.isWorking)
                {
                    //TypeSetter.networkObjects.Remove(this);
                    isDone = true;
                }
            }
        }

        public void Dispose()
        {
            Thread removeFromList = new Thread(RemoveFromAttributes);
            removeFromList.Start();
        }
    }
//#endif
}
