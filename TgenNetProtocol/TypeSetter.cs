using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

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
                    Type type = networkObject.GetType();

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

        private static void NetworkObjHandlerServer(ClientData clientInfo, IEnumerable<MethodInfo> methodsInfo, object message, INetworkObject networkObject, List<object> objectsToSend)
        {
            foreach (var method in methodsInfo)
            {
                //if (CheckMethodFirstParameterServer(method) == message.GetType()
                //|| CheckMethodFirstParameterServer(method) == typeof(object))
                if (CheckMethodFirstParameterServer(method).IsAssignableFrom(message.GetType()))
                {
                    if (IsGetClientInfo(method))
                    {
                        objectsToSend.Add(clientInfo);
                        networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray(), networkObject);
                        objectsToSend.Remove(clientInfo);
                    }
                    else
                    {
                        networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray(), networkObject);
                    }
                }
            }
        }

        private static Type CheckMethodFirstParameterServer(MethodInfo method) //ISSUE: throws an error when a client runs it, but works fine on both sides
        {
            var parameters = method.GetParameters().ToList();
            if (parameters.Count <= 2)//there must be one argument to the a client method
            {
                return parameters[0].ParameterType;
            }
            else
                //TgenLog.Log("A network receiver is only allowed to get one parameter and client info");
                throw new ArgumentOutOfRangeException("A network receiver is only allowed to get one parameter");
        }

        /// <summary>
        /// Checks if the method wants the id of the client
        /// </summary>
        /// <param name="method">The method</param>
        /// <returns></returns>
        private static bool IsGetClientInfo(MethodInfo method)
        {
            var parameters = method.GetParameters().ToList();
            if (parameters.Count >= 2)
            {
                if (parameters[1].ParameterType == typeof(ClientData))
                {
                    return true;
                }
                throw new ArgumentException("Expected an ClientData type argument for the second argument! The second argument must be of type ClientData!");
            }
            return false;
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
                    Type type = networkObject.GetType();
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

        private static void NetworkObjHandlerClient(IEnumerable<MethodInfo> methodsInfo, object message, INetworkObject networkObject, List<object> objectsToSend)
        {
            foreach (var method in methodsInfo)
            {
                //if (CheckMethodFirstParameterClient(method) == message.GetType()
                //|| CheckMethodFirstParameterClient(method) == typeof(object))
                if (CheckMethodFirstParameterClient(method).IsAssignableFrom(message.GetType()))
                {
                    networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray(), networkObject);
                }
            }
        }

        private static Type CheckMethodFirstParameterClient(MethodInfo method) //ISSUE: throws an error when a client runs it, but works fine on both sides
        {
            var parameters = method.GetParameters().ToList();
            //method.GetCustomAttributes(typeof(ServerNetworkReciverAttribute), false).FirstOrDefault().GetType()
            if ((parameters.Count <= 1))//there must be one argument to the a client method
            {
                return parameters[0].ParameterType;
            }
            else
                throw new ArgumentOutOfRangeException("A network receive message is only allowed to get one parameter");
        }
        #endregion
    }
}
