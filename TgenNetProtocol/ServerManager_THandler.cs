using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class ServerManager //TYPE HANDLER
    {
        private Dictionary<(Type type, ClientInfo sender), Stack<TaskCompletionSource<object>>> AwaitingRequests { get; set; } =
            new Dictionary<(Type type, ClientInfo sender), Stack<TaskCompletionSource<object>>>();


        /// <summary>Waits for the client to send the type</summary>
        /// <typeparam name="T">The awaiting type</typeparam>
        /// <param name="sender">The client to set the task for</param>
        /// <returns>A task that waits for the client to send the specified type</returns>
        public async Task<T> WaitFor<T>(ClientInfo sender) => (T)await WaitFor(typeof(T), sender);

        public Task<object> WaitFor(Type type, ClientInfo sender)
        {
            var taskSource = new TaskCompletionSource<object>();
            if (AwaitingRequests.TryGetValue((type, sender), out var reqs))
                reqs.Push(taskSource);
            else
            {
                var reqsStack = new Stack<TaskCompletionSource<object>>();
                reqsStack.Push(taskSource);
                AwaitingRequests.Add((type, sender), reqsStack);
            }
            return taskSource.Task;
        }

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<T> WaitFor<T>(ClientInfo client, int millisecondsTimeout)
        {
            Task<T> waitTask = WaitFor<T>(client);
            Task timeout = Task.Delay(millisecondsTimeout);
            Task result = Task.WhenAny(waitTask, timeout);
            return result == timeout ? default : await waitTask;
        }

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<object> WaitFor(Type type, ClientInfo client, int millisecondsTimeout)
        {
            Task<object> waitTask = WaitFor(type, client);
            Task timeout = Task.Delay(millisecondsTimeout);
            Task result = Task.WhenAny(waitTask, timeout);
            return result == timeout ? default : await waitTask;
        }

        private void OnPacket(object obj, ClientInfo client)
        {
            Type t = obj.GetType();
            if (AwaitingRequests.TryGetValue((t, client), out var requests))
            {
                foreach (var req in requests)
                    req.SetResult(obj);
                AwaitingRequests.Remove((t, client));
            }
        }
    }
}
