using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace TgenNetProtocol
{
    public abstract class NetworkBehavour : INetworkObject
    {
        public static readonly Dictionary<Type/*Object Type*/,
            Dictionary<Type /*Attribute type*/, List<MethodData> /*Methods of object*/>> TotalNetMethods = new Dictionary<Type, Dictionary<Type, List<MethodData>>>();

        protected Dictionary<Type /*Attriute type*/, List<MethodData> /*Methods*/> NetworkMethods = new Dictionary<Type, List<MethodData>>()
        {
                { typeof(ServerReceiverAttribute), new List<MethodData>() },
                { typeof(ClientReceiverAttribute), new List<MethodData>() },
                { typeof(DgramReceiverAttribute), new List<MethodData>() }
        };

        public List<MethodData> ServerMethods => NetworkMethods[typeof(ServerReceiverAttribute)];
        public List<MethodData> ClientMethods => NetworkMethods[typeof(ClientReceiverAttribute)];
        public List<MethodData> DgramMethods => NetworkMethods[typeof(DgramReceiverAttribute)];

        public NetworkBehavour()
        {
            SetUpMethods();

            Add2Attributes();
        }

        public virtual void SetUpMethods()
        {
            Type thisType = GetType();
            if (!TotalNetMethods.TryGetValue(thisType, out var typeMethods))
            {
                SetTypeMethods();
                return;
            }
            foreach (var attribute in typeMethods.Keys)
            {
                List<MethodData> newTargetedMethods = typeMethods[attribute].Select(method => method.ChangeTarget(this)).ToList();
                NetworkMethods[attribute] = newTargetedMethods;
            }
        }

        protected virtual void SetTypeMethods()
        {
            Type thisType = GetType();
            //Gets public/private/(public inherited) methods
            MethodInfo[] methods = thisType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod);

            foreach (var attribute in NetworkMethods.Keys)
            {
                var actions = methods.Where(x => x.GetCustomAttributes(attribute, false).FirstOrDefault() != null).ToList();
                var actionsData = GetMethodsData(actions);
                NetworkMethods[attribute] = actionsData;
            }

            TotalNetMethods.Add(thisType, NetworkMethods);
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
                    TypeSetter.networkObjects.Add(this);
                    isDone = true;
                }
            }
        }
        private void Add2Attributes()
        {
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
                foreach (var attribute in NetworkMethods.Keys)
                    NetworkMethods[attribute].Clear();
                NetworkMethods.Clear();
            }
        }

        private void Remove2Attributes()
        {
            int index = TypeSetter.networkObjects.IndexOf(this);
            if(index != -1) //Found
                TypeSetter.networkObjects[index] = null;

            foreach (var attribute in NetworkMethods.Keys)
                NetworkMethods[attribute].Clear();
            NetworkMethods.Clear();
        }

        public void Dispose() =>
            Remove2Attributes();//Task.Run(RemoveFromAttributes);

        public void InvokeNetworkMethods(MethodData method, object[] objetsToSend) =>
            method.Invoke(objetsToSend);
    }
}
