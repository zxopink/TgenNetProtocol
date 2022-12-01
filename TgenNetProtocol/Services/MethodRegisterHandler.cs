using System;
using System.Collections.Generic;
using System.Text;

namespace TgenNetProtocol
{
    internal class MethodRegisterHandler
    {
        private Dictionary<Type, List<MethodData>> RegisteredMethods { get; set; } =
            new Dictionary<Type, List<MethodData>>();

        internal void Register(Delegate meth)
        {
            MethodData data = (MethodData)meth;
            if (RegisteredMethods.TryGetValue(data.ParameterType, out var methods))
            {
                methods.Add(data);
            }
            else
            {
                List<MethodData> list = new List<MethodData>();
                list.Add(data);
                RegisteredMethods.Add(data.ParameterType, list);
            }
        }

        internal bool Unregister(Delegate meth)
        {
            MethodData data = (MethodData)meth;
            Type type = data.ParameterType;
            if (RegisteredMethods.TryGetValue(type, out var methods))
                for (int i = 0; i < methods.Count; i++)
                    if (methods[i] == data)
                    {
                        methods.RemoveAt(i);
                        if (methods.Count == 0)
                            RegisteredMethods.Remove(type);
                        return true;
                    }

            return false;
        }

        internal void CallRegisters(object message) =>
            CallRegisters(message, null);

        internal void CallRegisters(object message, IPeerInfo client)
        {
            Type t = message.GetType();
            if (RegisteredMethods.TryGetValue(t, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    MethodData meth = list[i];
                    meth.Invoke(message, client);
                }
            }
        }
    }
}
