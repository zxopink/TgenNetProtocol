using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace TestTgenProtocol.TcpTests
{
    internal class ServerSideTcp : NetworkBehavour
    {
        public int PrimitiveVal { get; set; }
        public string StringVal { get; set; }
        public TestTcpClass ManagedVal { get; set; }
        [ServerReceiver]
        public void RecvPrimitive(int val) =>
            this.PrimitiveVal = val;

        [ServerReceiver]
        public void RecvString(string val) =>
            this.StringVal = val;

        [ServerReceiver]
        public void RecvManaged(TestTcpClass val) =>
            this.ManagedVal = val;

        public async Task Received()
        {
            while (ManagedVal == default || PrimitiveVal == default || StringVal == default)
                await Task.Delay(50);
        }

    }
}
