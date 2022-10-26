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

        protected Dictionary<Type /*Attriute type*/, List<MethodData> /*Methods*/> NetworkMethods => new Dictionary<Type, List<MethodData>>()
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

            AddToAttributes();
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
                var actions = methods.Where(x => x.GetCustomAttributes(attribute, false).FirstOrDefault() != null);
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

        protected virtual void AddToAttributes()
        {
            TypeSetter.Add(this);
        }

        protected virtual void RemoveFromAttributes()
        {
            TypeSetter.Remove(this);
            NetworkMethods.Clear();
        }

        public virtual void Dispose() =>
            RemoveFromAttributes();//Task.Run(RemoveFromAttributes);

        public virtual void InvokeNetworkMethods(MethodData method, object[] objetsToSend) =>
            method.Invoke(objetsToSend);
    }
}
