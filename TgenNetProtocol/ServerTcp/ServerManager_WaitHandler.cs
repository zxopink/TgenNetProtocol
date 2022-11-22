using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class ServerManager //TYPE HANDLER
    {
        private WaitForHandler<(Type type, ClientInfo sender)> WaitForHandler { get; set; } 
            = new WaitForHandler<(Type type, ClientInfo sender)>();

        /// <summary>Waits for the client to send the type</summary>
        /// <typeparam name="T">The awaiting type</typeparam>
        /// <param name="sender">The client to set the task for</param>
        /// <returns>A task that waits for the client to send the specified type</returns>
        public async Task<T> WaitFor<T>(ClientInfo sender) => 
            (T)await WaitFor(typeof(T), sender);

        public Task<object> WaitFor(Type type, ClientInfo sender) =>
            WaitForHandler.WaitFor((type, sender));

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<T> WaitFor<T>(ClientInfo client, int millisecondsTimeout) =>
            (T) await WaitFor(typeof(T), client, millisecondsTimeout);

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public Task<object> WaitFor(Type type, ClientInfo client, int millisecondsTimeout) =>
            WaitForHandler.WaitFor((type, client), millisecondsTimeout);

        public async Task<T> WaitFor<T>(ClientInfo client, TimeSpan timeout) =>
            (T) await WaitFor(typeof(T), client, timeout);

        public Task<object> WaitFor(Type type, ClientInfo client, TimeSpan timeout) =>
            WaitForHandler.WaitFor((type, client), timeout);

        private void OnPacket(object obj, ClientInfo client) =>
            WaitForHandler.OnPacket((obj.GetType(), client), obj);
    }
}
