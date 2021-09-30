﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public class MethodData
    {
        private Delegate del;
        public Delegate Delegate { get => del; }

        private Type returnType;
        public Type ReturnType { get => returnType; }

        private Type[] argumentsType;
        /// <summary>
        /// All of the function's pararameters (can also be ClientData)
        /// </summary>
        public Type[] ArgumentsType { get => argumentsType; }

        /// <summary>
        /// The main argument of the function
        /// </summary>
        public Type ParameterType { get => ArgumentsType[0]; }

        private object parent;
        public object Parent { get => parent; }

        private MethodInfo method;
        public MethodInfo Method { get => method; }

        public bool hasClientData;
        public bool HasClientData { get => hasClientData; }
        public MethodData(MethodInfo method, object parent)
        {
            this.method = method; //Must be set first
            this.parent = parent;

            SetTypes(); //Must be called before delegate creation
            del = CreateDelegate(parent);
        }

        private void SetTypes()
        {
            ParameterInfo[] parameters = method.GetParameters();
            Type[] types = new Type[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
                types[i] = parameters[i].ParameterType;

            //Set argument types
            argumentsType = types;

            //Set return type
            returnType = method.ReturnType;

            //Check if method gets clientdata as parameter
            if (parameters.Length >= 2)
                hasClientData = parameters[1].ParameterType == typeof(ClientData);
        }

        private Delegate CreateDelegate(object parent)
        {
            int paramsAmount = argumentsType.Length + 1; //Parameters and return type
            Type[] args = new Type[paramsAmount];

            for (int i = 0; i < argumentsType.Length; i++)
                args[i] = argumentsType[i];
            args[argumentsType.Length] = returnType;

            var delDecltype = Expression.GetDelegateType(args);
            return Delegate.CreateDelegate(delDecltype, parent, method);
        }

        public object Invoke(object[] parameters) =>
            method.Invoke(parent, parameters);
    }
}