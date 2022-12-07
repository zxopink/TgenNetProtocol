using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class ClientManager
    {

        private WaitForHandler<Type> WaitForHandler { get; set; } 
            = new WaitForHandler<Type>();

        public async Task<T> WaitFor<T>() => 
            (T)await WaitFor(typeof(T));

        public Task<object> WaitFor(Type type) =>
            WaitForHandler.WaitFor(type);

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<T> WaitFor<T>(int millisecondsTimeout) =>
            (T) await WaitFor(typeof(T), millisecondsTimeout);

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public Task<object> WaitFor(Type type, int millisecondsTimeout) =>
            WaitForHandler.WaitFor(type, millisecondsTimeout);

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<T> WaitFor<T>(TimeSpan timeout) =>
            (T) await WaitFor(typeof(T), timeout);

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public Task<object> WaitFor(Type type, TimeSpan timeout) =>
            WaitForHandler.WaitFor(type, timeout);

        private void OnPacket(object obj) =>
            WaitForHandler.OnPacket(obj.GetType(), obj);
    }
}
