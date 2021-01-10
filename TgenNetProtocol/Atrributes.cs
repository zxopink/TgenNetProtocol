using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
//using UnityEngine;

namespace TgenNetProtocol
{
    #region Attributes
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerNetworkReciverAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientNetworkReciverAttribute : Attribute
    {
    }
    #endregion
    public class AttributeActions
    {
        public volatile static List<INetworkObject> networkObjects = new List<INetworkObject>(); //list of active networkObjects
        //public volatile static List<object> networkObjectsToRemove = new List<object>(); //list of network objects to remove from the active networkObjects list

        /// <summary>
        /// this bool let's other threads know if a message is being send
        /// the sending proccess takes time and cannot get changed at run-time (things might break)
        /// </summary>
        public volatile static bool isWorking = false;

        private static bool windowsForms = false, unity = false;
        /// <summary>
        /// Executing an assembly that's not in the project will result in a fatal exception
        /// </summary>
        public static void CheckAvailableAssemblies()
        {
            Assembly[] projectAssemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assemblyInProject in projectAssemblies)
            {
                if (string.Equals(assemblyInProject.GetName().Name, "System.Windows.Forms"))
                    windowsForms = true;

                if (string.Equals(assemblyInProject.GetName().Name, "UnityEngine"))
                    unity = true;
            }
        }

        #region Server Get Message
        public static void SendNewServerMessage(object message, int clientId)
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
                    //var methodsInfo = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ServerNetworkReciverAttribute), false).FirstOrDefault() != null);
                    //meaning the file doesn't exist in the project
                    //not a Windows project

                    //this part checks for form variables which cannot be touched from multiple threads
                    //so it handles multiple thread in forms

                    if (windowsForms)
                    {
                        //FormHandlerServer(clientId, methodsInfo, message, networkObject, objectsToSend);
                        NetworkObjHandlerServer(clientId, methodsInfo, message, networkObject, objectsToSend);
                    }

                    if (unity)
                    {
                        //UnityHandlerServer(clientId, methodsInfo, message, networkObject, objectsToSend);
                        NetworkObjHandlerServer(clientId, methodsInfo, message, networkObject, objectsToSend);
                    }

                    //this is for normal situations (CMD (console) for example)
                    else
                    {
                        NetworkObjHandlerServer(clientId, methodsInfo, message, networkObject, objectsToSend);
                        /*
                        foreach (var method in methodsInfo)
                        {
                            if (CheckMethodFirstParameterForServer(method) == message.GetType()
                            || CheckMethodFirstParameterForServer(method) == typeof(object))
                            {
                                try
                                {
                                    if (IsGetClientId(method))
                                    {
                                        objectsToSend.Add(clientId);
                                        method.Invoke(networkObject, objectsToSend.ToArray());
                                        objectsToSend.Remove(clientId);
                                    }
                                    else
                                    {
                                        method.Invoke(networkObject, objectsToSend.ToArray());
                                    }
                                }
                                catch (Exception e)
                                {
                                    TgenLog.Log("Server: Had issues invoking method " + method.Name);
                                    TgenLog.Log(e.ToString());
                                }
                            }
                        }
                        */
                    }
                }
                else
                    nullObjectsToRemove.Add(networkObject);
                    
            }
            //foreach (var removeObj in networkObjectsToRemove) //remove disposed objects
            //    networkObjects.Remove(removeObj);
            //networkObjectsToRemove.Clear();
            
            isWorking = false;

            foreach (var nullObj in nullObjectsToRemove) //remove null objects (occurs with MonoNetwork when Monobehaviour.Destroy() is called)
                networkObjects.Remove(nullObj);
            nullObjectsToRemove.Clear();
        }

        /// <summary>
        /// Since the project is multiplatform and works with Windows form, things might get messy when a different platfrom touches the windows forms
        /// to fix that, this method will only be invoked if forms exist in the current project and don't throw a 'FileNotFoundException'
        /// if the code touches a method that includes a type that isn't in the assembly, so program will only call this method IF forms can be found in the proeject
        /// and if not, it will safely avoid it
        /// </summary>
        private static void FormHandlerServer(object clientId, IEnumerable<MethodInfo> methodsInfo, object message, object networkObject, List<object> objectsToSend)
        {
            if (!(networkObject is FormNetworkBehavour))
                return;

            foreach (var method in methodsInfo)
            {
                if (CheckMethodFirstParameterForServer(method) == message.GetType()
                || CheckMethodFirstParameterForServer(method) == typeof(object))
                {
                    if (IsGetClientId(method))
                    {
                        objectsToSend.Add(clientId);
                        var netObj = (FormNetworkBehavour)networkObject;
                        netObj.InvokeSafely(method, objectsToSend.ToArray(), networkObject);
                        objectsToSend.Remove(clientId);
                    }
                    else
                    {
                        var netObj = (FormNetworkBehavour)networkObject;
                        netObj.InvokeSafely(method, objectsToSend.ToArray(), networkObject);
                    }
                }
            }
        }

        private static void UnityHandlerServer(object clientId, IEnumerable<MethodInfo> methodsInfo, object message, object networkObject, List<object> objectsToSend)
        {
            if (!(networkObject is MonoNetwork))
                return;

            foreach (var method in methodsInfo)
            {
                if (CheckMethodFirstParameterForServer(method) == message.GetType()
                || CheckMethodFirstParameterForServer(method) == typeof(object))
                {
                    if (IsGetClientId(method))
                    {
                        objectsToSend.Add(clientId);
                        var netObj = (MonoNetwork)networkObject;
                        netObj.InvokeSafely(method, objectsToSend.ToArray(), networkObject);
                        objectsToSend.Remove(clientId);
                    }
                    else
                    {
                        var netObj = (MonoNetwork)networkObject;
                        netObj.InvokeSafely(method, objectsToSend.ToArray(), networkObject);
                    }
                }
            }
        }

        private static void NetworkObjHandlerServer(object clientId, IEnumerable<MethodInfo> methodsInfo, object message, INetworkObject networkObject, List<object> objectsToSend)
        {
            foreach (var method in methodsInfo)
            {
                if (CheckMethodFirstParameterForServer(method) == message.GetType()
                || CheckMethodFirstParameterForServer(method) == typeof(object))
                {
                    if (IsGetClientId(method))
                    {
                        objectsToSend.Add(clientId);
                        networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray(), networkObject);
                        objectsToSend.Remove(clientId);
                    }
                    else
                    {
                        networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray(), networkObject);
                    }
                }
            }
        }

        private static Type CheckMethodFirstParameterForServer(MethodInfo method) //ISSUE: throws an error when a client runs it, but works fine on both sides
        {
            var parameters = method.GetParameters().ToList();
            if (parameters.Count <= 2)//there must be one argument to the a client method
            {
                return parameters[0].ParameterType;
            }
            else
                throw new ArgumentOutOfRangeException("A network receiver is only allowed to get one parameter");
        }
        /// <summary>
        /// Checks if the method wants the id of the client
        /// </summary>
        /// <param name="method">The method</param>
        /// <returns></returns>
        private static bool IsGetClientId(MethodInfo method)
        {
            var parameters = method.GetParameters().ToList();
            if (parameters.Count >= 2)
            {
                if (parameters[1].ParameterType == typeof(int) || parameters[1].ParameterType == typeof(ClientData))
                {
                    return true;
                }
                throw new ArgumentException("Expected an int/ClientData type argument for the second argument! The second argument must be of type int or ClientData!");
            }
            return false;
        }
        #endregion

        #region Client Get Message
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
                    //var methodsInfo = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ClientNetworkReciverAttribute), false).FirstOrDefault() != null);

                    //this part checks for form variables which cannot be touched from multiple threads
                    //so it handles multiple thread in forms
                    if (windowsForms)
                    {
                        //FormHandlerClient(methodsInfo, message, networkObject, objectsToSend);
                        NetworkObjHandlerClient(methodsInfo, message, networkObject, objectsToSend);
                    }
                    if (unity)
                    {
                        //UnityHandlerClient(methodsInfo, message, networkObject, objectsToSend);
                        NetworkObjHandlerClient(methodsInfo, message, networkObject, objectsToSend);
                    }
                    //this is for normal situtations (CMD (console) for example)
                    else
                    {
                        NetworkObjHandlerClient(methodsInfo, message, networkObject, objectsToSend);
                        /*
                        foreach (var method in methodsInfo)
                        {
                            if (CheckMethodFirstParameterForClient(method) == message.GetType()
                            || CheckMethodFirstParameterForClient(method) == typeof(object))
                            {
                                try
                                {
                                    method.Invoke(networkObject, objectsToSend.ToArray());
                                }
                                catch (Exception e)
                                {
                                    TgenLog.Log("Client: Had issues invoking method " + method.Name);
                                    TgenLog.Log(e.ToString());
                                }
                            }
                        }
                        */
                    }
                }
                else
                    nullObjectsToRemove.Add(networkObject);
            }
            //foreach (var removeObj in networkObjectsToRemove) //remove disposed objects
            //    networkObjects.Remove(removeObj);
            //networkObjectsToRemove.Clear();

            isWorking = false;

            foreach (var nullObj in nullObjectsToRemove)
                networkObjects.Remove(nullObj);
            nullObjectsToRemove.Clear();
        }

        private static void FormHandlerClient(IEnumerable<MethodInfo> methodsInfo, object message, object networkObject, List<object> objectsToSend)
        {
            if (!(networkObject is FormNetworkBehavour))
                return;

            foreach (var method in methodsInfo)
            {
                if (CheckMethodFirstParameterForClient(method) == message.GetType()
                || CheckMethodFirstParameterForClient(method) == typeof(object))
                {
                    var netObj = (FormNetworkBehavour)networkObject;
                    netObj.InvokeSafely(method, objectsToSend.ToArray(), networkObject);
                }
            }
        }

        private static void UnityHandlerClient(IEnumerable<MethodInfo> methodsInfo, object message, object networkObject, List<object> objectsToSend)
        {
            if (!(networkObject is MonoNetwork))
                return;

            foreach (var method in methodsInfo)
            {
                if (CheckMethodFirstParameterForClient(method) == message.GetType()
                || CheckMethodFirstParameterForClient(method) == typeof(object))
                {
                    var netObj = (MonoNetwork)networkObject;
                    netObj.InvokeSafely(method, objectsToSend.ToArray(), networkObject);
                }
            }
        }

        private static void NetworkObjHandlerClient(IEnumerable<MethodInfo> methodsInfo, object message, INetworkObject networkObject, List<object> objectsToSend)
        {
            foreach (var method in methodsInfo)
            {
                if (CheckMethodFirstParameterForClient(method) == message.GetType()
                || CheckMethodFirstParameterForClient(method) == typeof(object))
                {
                    networkObject.InvokeNetworkMethods(method, objectsToSend.ToArray(), networkObject);
                }
            }
        }

        private static Type CheckMethodFirstParameterForClient(MethodInfo method) //ISSUE: throws an error when a client runs it, but works fine on both sides
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

        /// <summary>
        /// this method checks the first parameter type and returns it
        /// it will throw an error if there's more than one parameter as it was not intended
        /// remember: one message per network packet
        /// </summary>
        /// <returns></returns>
        private static Type CheckMethodFirstParameter(MethodInfo method) //ISSUE: throws an error when a client runs it, but works fine on both sides
        {
            var parameters = method.GetParameters().ToList();
            //method.GetCustomAttributes(typeof(ServerNetworkReciverAttribute), false).FirstOrDefault().GetType()
            if ((method.GetCustomAttributes(typeof(ServerNetworkReciverAttribute), false).FirstOrDefault().GetType() == typeof(ServerNetworkReciverAttribute) && parameters.Count <= 2) ||//there must be at least two arguments in server method
                (method.GetCustomAttributes(typeof(ClientNetworkReciverAttribute), false).FirstOrDefault().GetType() == typeof(ClientNetworkReciverAttribute) && parameters.Count <= 1))//there must be one argument to the a client method
            {
                return parameters[0].ParameterType;
            }
            else
                throw new ArgumentOutOfRangeException("A network receive message is only allowed to get one parameter");
        }

        private static Type CheckMethodParameter(MethodInfo method, int parameterSlot)
        {
            var parameters = method.GetParameters().ToList();
            if (parameterSlot < parameters.Count)
                return parameters[parameterSlot].ParameterType;
            else
                throw new ArgumentOutOfRangeException("you tried to reach an argument that doesn't exist");
        }
    }
}
