using System;
using System.Collections.Generic;
using System.Text;

namespace TgenNetProtocol
{
    public partial class ServerManager<ClientsType>
    {
        private MethodRegisterHandler MethodRegister = new MethodRegisterHandler();

        public void Register<T>(Action<T, ClientsType> method) => 
            MethodRegister.Register(method);
        public void Register<T>(Action<T> method) =>
            MethodRegister.Register(method);
        //private void Register(Delegate meth) =>
        //    MethodRegister.Register(meth);

        public void Unregister<T>(Action<T, ClientsType> method) =>
            MethodRegister.Unregister(method);
        public void Unregister<T>(Action<T> method) =>
            MethodRegister.Unregister(method);
        //private bool Unregister(Delegate meth) =>
        //    MethodRegister.Unregister(meth);

        private void CallRegisters(object message, ClientsType client) =>
            MethodRegister.CallRegisters(message, client);
    }
}
