using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public class MethodData
    {
        private dynamic _method;

        public Delegate Method
        {
            get => _method;
            private set => _method = value;
        }

        private Type _methodType;
        private Type MethodType => _methodType;

        private Type _parameterType;
        /// <summary>The main argument of the function</summary>
        public Type ParameterType { get => _parameterType; }

        private bool _hasClientData;
        ///<summary>If true, The function's second parameter is INetInfo</summary>
        public bool HasClientData { get => _hasClientData; }

        public MethodData(MethodInfo methodInfo, object parent)
        {
            var param = methodInfo.GetParameters();
            _hasClientData = param.Length > 1;

            //TODO: Remove once code analyzers are in place
            var exception = CheckErrors(methodInfo, param);
            if (exception != null)
                throw exception;

            Method = CreateDelegate(methodInfo, parent);

            _parameterType = param[0].ParameterType;
        }

        private MethodData(MethodData otherData, object parent)
        {
            _methodType = otherData.MethodType;
            _parameterType = otherData.ParameterType;
            _hasClientData = otherData.HasClientData;

            Delegate otherMethod = otherData.Method;
            Method = otherMethod.Method.CreateDelegate(MethodType, parent);
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
                new Type[] { param[0].ParameterType, param[1].ParameterType, info.ReturnType } :
                new Type[] { param[0].ParameterType, info.ReturnType };

            _methodType = Expression.GetDelegateType(args);
            return info.CreateDelegate(_methodType, parent);
        }

        public MethodData ChangeTarget(object target)
        {
            return new MethodData(this, target);
        }

        /// <summary>Invokes the function</summary>
        /// <param name="parameters">The functions parameters</param>
        public void Invoke(dynamic[] parameters)
        {
            if (HasClientData)
                _method(parameters[0], parameters[1]);
            else
                _method(parameters[0]);
        }

        public override bool Equals(object obj)
        {
            if (obj is Delegate del)
            {
                return Method.Equals(del);
            }
            else if (obj is MethodData data)
            {
                return Method.Equals(data.Method);
            }
            return false;
        }

        public static bool operator ==(MethodData a, MethodData b) => a.Equals(b);
        public static bool operator !=(MethodData a, MethodData b) => !a.Equals(b);

        public void Invoke(dynamic netObject) =>
            _method(netObject);
        public void Invoke(dynamic netObject, INetInfo netInfo) =>
            _method(netObject, (dynamic)netInfo);

        public static explicit operator MethodData(Delegate method) =>
            new MethodData(method.Method, method.Target ?? throw new NullReferenceException("MethodData's parent cannot be null"));

        //objects must be dynamic, the run-time doesn't look at the object type but the variable type
        //Best way is to keep the variable dynamic
    }
}
