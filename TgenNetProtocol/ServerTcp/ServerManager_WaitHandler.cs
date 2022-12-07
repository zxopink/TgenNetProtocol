using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class ServerManager<ClientsType> //TYPE HANDLER
    {
        private WaitForHandler<(Type type, ClientsType sender)> WaitForHandler { get; set; } 
            = new WaitForHandler<(Type type, ClientsType sender)>();

        /// <summary>Waits for the client to send the type</summary>
        /// <typeparam name="T">The awaiting type</typeparam>
        /// <param name="sender">The client to set the task for</param>
        /// <returns>A task that waits for the client to send the specified type</returns>
        public async Task<T> WaitFor<T>(ClientsType sender) => 
            (T)await WaitFor(typeof(T), sender);

        public Task<object> WaitFor(Type type, ClientsType sender) =>
            WaitForHandler.WaitFor((type, sender));

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<T> WaitFor<T>(ClientsType client, int millisecondsTimeout) =>
            (T) await WaitFor(typeof(T), client, millisecondsTimeout);

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public Task<object> WaitFor(Type type, ClientsType client, int millisecondsTimeout) =>
            WaitForHandler.WaitFor((type, client), millisecondsTimeout);

        public async Task<T> WaitFor<T>(ClientsType client, TimeSpan timeout) =>
            (T) await WaitFor(typeof(T), client, timeout);

        public Task<object> WaitFor(Type type, ClientsType client, TimeSpan timeout) =>
            WaitForHandler.WaitFor((type, client), timeout);

        private void OnPacket(object obj, ClientsType client) =>
            WaitForHandler.OnPacket((obj.GetType(), client), obj);
    }
}
