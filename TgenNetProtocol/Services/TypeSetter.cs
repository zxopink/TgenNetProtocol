using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;

namespace TgenNetProtocol
{
    /// <summary>Singleton type used by network manager objects</summary>
    public static class TypeSetter
    {
        private static List<INetworkObject> NetworkObjects { get; set; } 
            = new List<INetworkObject>(); //list of active networkObjects

        public static void Add(INetworkObject obj)
        {
            lock (NetworkObjects)
                NetworkObjects.Add(obj);
        }

        public static void Remove(INetworkObject obj)
        {
            lock (NetworkObjects)
                NetworkObjects.Remove(obj);
        }

        internal static void SendNewNetMessage(object message, INetManager caller) =>
            SendNewNetMessage(message, caller, clientInfo: null);


        /// <summary>
        /// Called when a new packet is received.
        /// This method invokes network methods on all active network objects under the given INetManager
        /// </summary>
        /// <param name="message"></param>
        /// <param name="caller"></param>
        /// <param name="clientInfo"></param>
        internal static void SendNewNetMessage(object message, INetManager caller, IPeerInfo clientInfo)
        {
            lock (NetworkObjects)
                for (int i = 0; i < NetworkObjects.Count; i++)
                {
                    INetworkObject networkObject = NetworkObjects[i];
                    if (networkObject == default
                        || (networkObject.NetManagers.Length != 0 && !networkObject.NetManagers.Contains(caller)))
                        continue;

                    object[] parameters = (clientInfo == null) ?
                        new[] { message } : new[] { message, clientInfo };
                    // get method by name,  or loop through all methods
                    // looking for an attribute
                    List<MethodData> methodsInfo = GetNetMethods(networkObject, caller);
                    NetworkObjHandler(methodsInfo, parameters, networkObject);
                }
        }

        private static List<MethodData> GetNetMethods(INetworkObject netObject, INetManager caller)
        {
            if (caller is ClientManager)
                return netObject.ClientMethods;
            else if (caller is IServerManager)
                return netObject.ServerMethods;
#if IncludeUDP
            else if (caller is UdpManager)
                return netObject.DgramMethods;
#endif
            else
                throw new ArgumentException($"{nameof(caller)} is not a valid NetManager type: {caller.GetType()}");
        }

        private static void NetworkObjHandler(List<MethodData> methodsInfo, object[] parameters, INetworkObject networkObject)
        {
            foreach (var method in methodsInfo)
                if (method.ParameterType.IsAssignableFrom(parameters[0].GetType()))
                    networkObject.InvokeNetworkMethods(method, parameters);
        }

        //private static void NetworkObjHandler(List<MethodData> methodsInfo, object message)
        //{
        //    foreach (var method in methodsInfo)
        //        if (method.ParameterType.IsAssignableFrom(message.GetType()))
        //            method.Invoke(message);
        //}
    }
}