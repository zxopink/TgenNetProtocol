using System;
using System.Collections.Generic;
using System.Reflection;

namespace TgenNetProtocol
{
    public interface INetworkObject : IDisposable
    {
        IEnumerable<MethodInfo> ServerMethods { get; }
        IEnumerable<MethodInfo> ClientMethods { get; }

        void SetUpMethods();

        void InvokeNetworkMethods(MethodInfo method, object[] objetsToSend, object ObjectThatOwnsTheMethod);

        /// <summary>
        /// Dispose stops the broadcast for this object
        /// this object won't recive any of the incoming packets and it's methods won't be invoked
        /// </summary>
        new void Dispose();
    }
}
