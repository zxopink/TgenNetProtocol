using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;

namespace TgenNetProtocol.Services
{
    public class DynamicMethodException : Exception
    {
        public MethodData MethodData { get; }
        public Delegate Delegate => MethodData.Method;
        public MethodInfo Method => Delegate.Method;

        /// <summary> Gets the class instance on which the current delegate invokes the instance method.</summary>
        public INetworkObject Target => Delegate.Target as INetworkObject;

        public override string StackTrace => InnerException.StackTrace + "\r\n" + base.StackTrace;

        public DynamicMethodException(MethodData method, Exception inner) : base(inner.Message, inner)
        {
            MethodData = method;
        }
    }
}
