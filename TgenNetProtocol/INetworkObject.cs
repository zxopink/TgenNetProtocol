using System;
using System.Collections.Generic;
using System.Reflection;

namespace TgenNetProtocol
{
    /// <summary>For classes like ServerManager, ClientManger and UdpManager</summary>
    public interface INetManager { }
    public interface INetworkObject : IDisposable
    {
        List<MethodData> ServerMethods { get; }
        List<MethodData> ClientMethods { get; }
        List<MethodData> DgramMethods { get; }

        INetManager[] NetManagers { get; }

        void SetUpMethods();

        void InvokeNetworkMethods(MethodData method, object[] objetsToSend);

        /// <summary>
        /// Dispose stops the broadcast for this object.
        /// It won't recive any of the incoming packets and it's methods won't be invoked
        /// </summary>
        new void Dispose();
    }
}
