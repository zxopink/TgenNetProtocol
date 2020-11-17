﻿using System;
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
        public volatile static List<object> networkObjects = new List<object>();

        /// <summary>
        /// this bool let's other threads know if a message is being send
        /// the sending proccess takes time and cannot get changed at run-time (things might break)
        /// </summary>
        //might need to remove the "volatile" keyword, check on that
        public volatile static bool isWorking = false;

        private static List<Type> GetAllTypes()
        {
            List<Type> allNetworkTypes = new List<Type>();

            List<string> allNetworkTypesStr = new List<string>();
            allNetworkTypesStr.Add("TgenNetProtocol.NetworkBehavour");
            allNetworkTypesStr.Add("TgenNetProtocol.FormNetworkBehavour");
            allNetworkTypesStr.Add("TgenNetProtocol.MonoNetwork");

            for (int i = 0; i < allNetworkTypesStr.Count; i++)
            {
                try { allNetworkTypes.Add(Type.GetType(allNetworkTypesStr[i], true)); }
                catch { /*Console.WriteLine("could not find " + allNetworkTypesStr[i], true); */ }
            }
            return allNetworkTypes;
        }
        #region Server Get Message
        public static void SendNewServerMessage(object message, int clientId)
        {
            isWorking = true;
            List<object> objectsToSend = new List<object>();
            objectsToSend.Add(message);
            foreach (var networkObject in networkObjects)
            {
                if (networkObject != null)
                {
                    Type type = networkObject.GetType();

                    // get method by name,  or loop through all methods
                    // looking for an attribute
                    var methodsInfo = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ServerNetworkReciverAttribute), false).FirstOrDefault() != null);

                    //meaning the file doesn't exist in the project
                    //not a Windows project

                    //this part checks for form variables which cannot be touched from multiple threads
                    //so it handles multiple thread in forms

                    //if (networkObject is FormNetworkBehavour)
                    //if (networkObject.GetType().IsAssignableFrom(Type.GetType("TgenNetProtocol.FormNetworkBehavour")))
                    if (networkObject.GetType().IsSubclassOf(Type.GetType("TgenNetProtocol.FormNetworkBehavour")))
                    {
                        FormHandlerServer(clientId, methodsInfo, message, networkObject, objectsToSend);
                    }

                    /*
                    else if (networkObject is MonoNetwork)
                    //else if(networkObject.GetType().IsAssignableFrom(Type.GetType("TgenNetProtocol.MonoNetwork")))
                    {
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
                    */

                    //this is for normal situations (CMD (console) for example)
                    //if(networkObject is NetworkBehavour)
                    else
                    {
                        foreach (var method in methodsInfo)
                        {
                            if (CheckMethodFirstParameterForServer(method) == message.GetType()
                            || CheckMethodFirstParameterForServer(method) == typeof(object))
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
                        }
                    }
                }
                else
                    networkObjects.Remove(networkObject);
            }
            isWorking = false;
        }

        /// <summary>
        /// Since the project is multiplatform and works with Windows form, things might get messy when a different platfrom touches the windows forms
        /// to fix that, this method will only be invoked if forms exist in the current project and don't throw a 'FileNotFoundException'
        /// if the code touches a method that includes a type that isn't in the assembly, so program will only call this method IF forms can be found in the proeject
        /// and if not, it will safely avoid it
        /// </summary>
        private static void FormHandlerServer(int clientId, IEnumerable<MethodInfo> methodsInfo, object message, object networkObject, List<object> objectsToSend)
        {
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

        private static Type CheckMethodFirstParameterForServer(MethodInfo method) //ISSUE: throws an error when a client runs it, but works fine on both sides
        {
            var parameters = method.GetParameters().ToList();
            //method.GetCustomAttributes(typeof(ServerNetworkReciverAttribute), false).FirstOrDefault().GetType()
            if (parameters.Count <= 2)//there must be one argument to the a client method
            {
                return parameters[0].ParameterType;
            }
            else
                throw new ArgumentOutOfRangeException("A network receive message is only allowed to get one parameter");
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
                if (parameters[1].ParameterType == typeof(int))
                {
                    return true;
                }
                throw new ArgumentException("Expected an int type argument for the second argument to give the client id! The second argument must be type of int!");
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
            foreach (var networkObject in networkObjects)
            {
                if (networkObject != null)
                {
                    Type type = networkObject.GetType();

                    // get method by name,  or loop through all methods
                    // looking for an attribute
                    var methodsInfo = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ClientNetworkReciverAttribute), false).FirstOrDefault() != null);

                    //this part checks for form variables which cannot be touched from multiple threads
                    //so it handles multiple thread in forms

                    //if (networkObject is FormNetworkBehavour)
                    //if (networkObject.GetType().IsAssignableFrom(Type.GetType("TgenNetProtocol.FormNetworkBehavour")))
                    if (networkObject.GetType().IsSubclassOf(Type.GetType("TgenNetProtocol.FormNetworkBehavour")))
                    {
                        FormHandlerClient(methodsInfo, message, networkObject, objectsToSend);
                        /*
                        foreach (var method in methodsInfo)
                        {
                            if (CheckMethodFirstParameterForClient(method) == message.GetType()
                            || CheckMethodFirstParameterForClient(method) == typeof(object))
                            {
                                var netObj = (FormNetworkBehavour)networkObject;
                                netObj.InvokeSafely(method, objectsToSend.ToArray(), networkObject);
                            }
                        }
                        */
                    }
                    /*
                    else if (networkObject is MonoNetwork)
                    //else if(networkObject.GetType().IsAssignableFrom(Type.GetType("TgenNetProtocol.MonoNetwork")))
                    {
                        Console.WriteLine("failed to detect the correct one");
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
                    */
                    //this is for normal situtations (CMD (console) for example)
                    //if(networkObject is NetworkBehavour)
                    else
                    {
                        Console.WriteLine("failed to detect the correct one");
                        foreach (var method in methodsInfo)
                        {
                            if (CheckMethodFirstParameterForClient(method) == message.GetType()
                            || CheckMethodFirstParameterForClient(method) == typeof(object))
                                method.Invoke(networkObject, objectsToSend.ToArray());
                        }
                    }
                }
                else
                    networkObjects.Remove(networkObject);
            }
            isWorking = false;
        }

        public static void FormHandlerClient(IEnumerable<MethodInfo> methodsInfo, object message, object networkObject, List<object> objectsToSend)
        {
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
