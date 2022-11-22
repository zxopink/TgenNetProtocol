using System;
using System.Collections.Generic;
using System.Text;

namespace TgenNetProtocol
{
    public partial class ServerManager
    {
        private MethodRegisterHandler MethodRegister = new MethodRegisterHandler();

        public void Register<T>(Action<T, ClientInfo> method) => 
            MethodRegister.Register(method);
        public void Register<T>(Action<T> method) =>
            MethodRegister.Register(method);

        public void Unregister<T>(Action<T, ClientInfo> method) =>
            MethodRegister.Unregister(method);
        public void Unregister<T>(Action<T> method) =>
            MethodRegister.Unregister(method);

        private void CallRegisters(object message, ClientInfo client) =>
            MethodRegister.CallRegisters(message, client);
    }
}
