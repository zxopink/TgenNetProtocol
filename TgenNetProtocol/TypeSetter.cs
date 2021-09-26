using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace TgenNetProtocol
{
    public class TypeSetter
    {
        public volatile static List<INetworkObject> networkObjects = new List<INetworkObject>(); //list of active networkObjects

        /// <summary>
        /// this bool lets other threads know if a message is being send
        /// the sending proccess takes time and cannot get changed at run-time (things might break)
        /// </summary>
        public volatile static bool isWorking = false;

        #region Server Get Message
        /// <summary>
        /// Called when a packet is received from a client
        /// this method invokes server network methods on all active network objects
        /// </summary>
        /// <param name="message">The sent object (Payload)</param>
        /// <param name="clientInfo">The client who sent the info</param>
        public static void SendNewServerMessage(object message, ClientData clientInfo)
        {
            isWorking = true;
            List<object> objectsToSend = new List<object>();
            objectsToSend.Add(message);

            List<INetworkObject> nullObjectsToRemove = new List<INetworkObject>();
            foreach (var networkObject in networkObjects)
            {
                if (networkObject != null)
                {
                    // get method by name,  or loop through all methods
                    // looking for an attribute
                    var methodsInfo = networkObject.ServerMethods;
                    NetworkObjHandlerServer(clientInfo, methodsInfo, message, networkObject, objectsToSend);
                }
                else
                    nullObjectsToRemove.Add(networkObject);
            }
            isWorking = false;

            foreach (var nullObj in nullObjectsToRemove) //remove null objects (occurs with MonoNetwork when Monobehaviour.Destroy() is called)
                networkObjects.Remove(nullObj);
            nullObjectsToRemove.Clear();
        }

        private static void NetworkObjHandlerServer(ClientData clientInfo, List<MethodData> methodsInfo, object message, INetworkObject networkObject, List<object> objectsToSend)
        {
            foreach (var method in methodsInfo)
            {
                //if (CheckMethodFirstParameterServer(method) == message.GetType()
                //|| CheckMethodFirstParameterServer(method) == typeof(object))
                if (method.ParameterType.IsAssignableFrom(message.GetType()))
                {
                    if (method.hasClientData)
                    {
                        objectsToSend.Add(clientInfo);
                        networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray());
                        objectsToSend.Remove(clientInfo);
                    }
                    else
                    {
                        networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray());
                    }
                }
            }
        }
        #endregion

        #region Client Get Message
        /// <summary>
        /// Called when a packet is received from the server
        /// this method invokes client network methods on all active network objects
        /// </summary>
        /// <param name="message">The sent object (Payload)</param>
        public static void SendNewClientMessage(object message)
        {
            isWorking = true;
            List<object> objectsToSend = new List<object>();
            objectsToSend.Add(message);

            List<INetworkObject> nullObjectsToRemove = new List<INetworkObject>();
            foreach (var networkObject in networkObjects)
            {
                if (networkObject != null)
                {
                    // get method by name,  or loop through all methods
                    // looking for an attribute
                    var methodsInfo = networkObject.ClientMethods;

                    NetworkObjHandlerClient(methodsInfo, message, networkObject, objectsToSend);
                }
                else
                    nullObjectsToRemove.Add(networkObject);
            }
            isWorking = false;

            foreach (var nullObj in nullObjectsToRemove)
                networkObjects.Remove(nullObj);
            nullObjectsToRemove.Clear();
        }

        private static void NetworkObjHandlerClient(List<MethodData> methodsInfo, object message, INetworkObject networkObject, List<object> objectsToSend)
        {
            foreach (var method in methodsInfo)
            {
                //if (CheckMethodFirstParameterClient(method) == message.GetType()
                //|| CheckMethodFirstParameterClient(method) == typeof(object))
                if (method.ParameterType.IsAssignableFrom(message.GetType()))
                {
                    networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray());
                }
            }
        }
        #endregion
    }
}
