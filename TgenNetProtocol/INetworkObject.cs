using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public interface INetworkObject : IDisposable
    {
        IEnumerable<MethodInfo> ServerMethods { get; }
        IEnumerable<MethodInfo> ClientMethods { get; }

        void SetUpMethods();

        void InvokeNetworkMethods(MethodInfo method, object[] objetsToSend, object ObjectThatOwnsTheMethod);

        new void Dispose();
    }
}
