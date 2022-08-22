using System;
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
        internal delegate void NetMethodInfo(dynamic netObject, INetInfo info);
        internal delegate void NetMethod(dynamic netObject);

        private dynamic method;

        public Delegate Method
        {
            get => method;
            private set => method = value;
        }

        private Type parameterType;
        /// <summary>The main argument of the function</summary>
        public Type ParameterType { get => parameterType; }

        private bool hasClientData;
        ///<summary>If true, The function's second parameter is INetInfo</summary>
        public bool HasClientData { get => hasClientData; }

        public MethodData(MethodInfo methodInfo, object parent)
        {
            var param = methodInfo.GetParameters();
            hasClientData = param.Length > 1;

            Method = CreateDelegate(methodInfo, parent);

            //TODO: Remove once code analyzers are in place
            var exception = CheckErrors(methodInfo, param);
            if (exception != null)
                throw exception;

            parameterType = param[0].ParameterType;
        }

        private ArgumentException CheckErrors(MethodInfo info, ParameterInfo[] parameters)
        {
            string name = info.Name;

            if(info.ReturnType != typeof(void))
                return new ArgumentException($"'{name}' return value has to be void");

            if (parameters.Length == 0)
                return new ArgumentException($"'{name}' doesn't accept any parameters");

            if (parameters.Length > 2)
                return new ArgumentException($"'{name}' has more than 2 parameters");

            if (parameters.Length > 1 && !typeof(INetInfo).IsAssignableFrom(parameters[1].ParameterType))
                return new ArgumentException($"'{name}' second parameter is not of type {typeof(INetInfo)}");

            return null;
        }

        private Delegate CreateDelegate(MethodInfo info, object parent)
        {
            var param = info.GetParameters();

            //Parameters and return type //[parameter,return] or [parameter, netinfo, return]
            Type[] args = HasClientData ?
                new Type[] { param[0].ParameterType, param[1].ParameterType, typeof(void) } :
                new Type[] { param[0].ParameterType, typeof(void) };

            var delDecltype = Expression.GetDelegateType(args);
            return info.CreateDelegate(delDecltype, parent);
        }

        /// <summary>Invokes the function</summary>
        /// <param name="parameters">The functions parameters</param>
        public void Invoke(object[] parameters)
        {
            if (HasClientData)
                method(parameters[0], (INetInfo)parameters[1]);
            else
                method(parameters[0]);
        }

        public void Invoke(object netObject) =>
            method(netObject);
        public void Invoke(object netObject, INetInfo netInfo) =>
            method(netObject, netInfo);
    }
}
