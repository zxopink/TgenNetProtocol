﻿using System;
using System.Collections.Generic;
using System.Text;

namespace TgenNetProtocol
{
    public partial class ClientManager
    {
        private MethodRegisterHandler MethodRegister { get; set; } = new MethodRegisterHandler();

        public void Register<T>(Action<T> method) => Register((Delegate)method);
        private void Register(Delegate meth) =>
            MethodRegister.Register(meth);

        public void Unregister<T>(Action<T> method) => Unregister((Delegate)method);
        private bool Unregister(Delegate meth) =>
            MethodRegister.Unregister(meth);

        private void CallRegisters(object message) =>
            MethodRegister.CallRegisters(message);
    }
}