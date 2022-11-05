using System;
using System.Collections.Generic;
using System.Reflection;

namespace TgenNetProtocol
{
    //Classes like ServerManager, ClientManger and UdpManager
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
        /// Dispose stops the broadcast for this object
        /// this object won't recive any of the incoming packets and it's methods won't be invoked
        /// </summary>
        new void Dispose();
    }
}
