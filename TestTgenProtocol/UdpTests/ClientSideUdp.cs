using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TgenNetProtocol;

namespace TestTgenProtocol.UdpTests
{
    internal class ClientSideUdp : NetworkBehavour
    {
        public int PrimitiveVal { get; set; }
        public string StringVal { get; set; }
        public TestUdpClass ManagedVal { get; set; }
        [DgramReceiver]
        public void RecvPrimitive(int val) =>
            this.PrimitiveVal = val;

        [DgramReceiver]
        public void RecvString(string val) =>
            this.StringVal = val;

        [DgramReceiver]
        public void RecvManaged(TestUdpClass val) =>
            this.ManagedVal = val;

        public async Task Received()
        {
            while (ManagedVal == default || PrimitiveVal == default || StringVal == default)
                await Task.Delay(50);
        }

        public void Clear()
        {
            ManagedVal = default;
            PrimitiveVal = default;
            StringVal = default;
        }
    }
}
