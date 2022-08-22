using System.Collections.Generic;

namespace TgenNetProtocol
{
    public class TypeSetter
    {
        public volatile static List<INetworkObject> networkObjects = new List<INetworkObject>(); //list of active networkObjects

        /// <summary>
        /// this bool lets other threads know if a message is being send
        /// the sending process takes time and cannot get changed at run-time (things might break)
        /// </summary>
        public volatile static bool isWorking = false;

        public void Add(INetworkObject obj)
        {
            networkObjects.Add(obj);
        }

        public void Remove(INetworkObject obj)
        {
            int index = networkObjects.IndexOf(obj);
            if (index != -1) //Found
                networkObjects[index] = null;
        }

        #region Server Get Message
        /// <summary>
        /// Called when a packet is received from a client
        /// this method invokes server network methods on all active network objects
        /// </summary>
        /// <param name="message">The sent object (Payload)</param>
        /// <param name="clientInfo">The client who sent the info</param>
        internal static void SendNewServerMessage(object message, ClientInfo clientInfo)
        {
            for (int i = 0; i < networkObjects.Count; i++)
            {
                INetworkObject networkObject = networkObjects[i];
                if (networkObject == null)
                    continue;

                // get method by name,  or loop through all methods
                // looking for an attribute
                var methodsInfo = networkObject.ServerMethods;
                NetworkObjHandler(clientInfo, methodsInfo, message, networkObject);
            }
            CleanNullObjects();
        }
        #endregion

        #region Client Get Message
        /// <summary>
        /// Called when a packet is received from the server
        /// this method invokes client network methods on all active network objects
        /// </summary>
        /// <param name="message">The sent object (Payload)</param>
        internal static void SendNewClientMessage(object message)
        {
            for (int i = 0; i < networkObjects.Count; i++)
            {
                INetworkObject networkObject = networkObjects[i];
                if (networkObject == null)
                    continue;

                // get method by name,  or loop through all methods
                // looking for an attribute
                var methodsInfo = networkObject.ClientMethods;

                NetworkObjHandler(methodsInfo, message, networkObject);
            }
            CleanNullObjects();
        }
        #endregion

        #region Datagram Get Message
        /// <summary>
        /// Called when a packet is received from a client
        /// this method invokes server network methods on all active network objects
        /// </summary>
        /// <param name="message">The sent object (Payload)</param>
        /// <param name="packetData">The client who sent the info</param>
        internal static void SendNewDatagramMessage(object message, UdpInfo packetData)
        {
            for (int i = 0; i < networkObjects.Count; i++)
            {
                INetworkObject networkObject = networkObjects[i];
                if (networkObject == null)
                    continue;

                // get method by name,  or loop through all methods
                // looking for an attribute
                var methodsInfo = networkObject.DgramMethods;
                NetworkObjHandler(packetData, methodsInfo, message, networkObject);
            }
            CleanNullObjects();
        }
        #endregion

        private static void NetworkObjHandler(INetInfo netInfo, List<MethodData> methodsInfo, object message, INetworkObject networkObject)
        {
            foreach (var method in methodsInfo)
            {
                if (method.ParameterType.IsAssignableFrom(message.GetType()))
                {
                    if (method.HasClientData)
                        networkObject.InvokeNetworkMethods(method, new object[] { message, netInfo });
                    else
                        networkObject.InvokeNetworkMethods(method, new object[] { message });
                }
            }
        }
        private static void NetworkObjHandler(List<MethodData> methodsInfo, object message, INetworkObject networkObject)
        {
            foreach (var method in methodsInfo)
                if (method.ParameterType.IsAssignableFrom(message.GetType()))
                    networkObject.InvokeNetworkMethods(method, new object[] { message });
        }

        private static void CleanNullObjects() =>
            networkObjects.RemoveAll(item => item == null);
    }
}