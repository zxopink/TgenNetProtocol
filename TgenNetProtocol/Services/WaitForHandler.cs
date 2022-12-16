using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    internal class WaitForHandler<TKey>
    {
        public Dictionary<TKey, TaskCompletionSource<object>> AwaitingRequests { get; private set; }

        public WaitForHandler()
        {
            AwaitingRequests = new Dictionary<TKey, TaskCompletionSource<object>>();
        }

        public Task<object> WaitFor(TKey key)
        {
            if (AwaitingRequests.TryGetValue(key, out var waitingTask))
                return waitingTask.Task;
            else
            {
                var taskSource = new TaskCompletionSource<object>();
                AwaitingRequests.Add(key, taskSource);
                return taskSource.Task;
            }
        }

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public Task<object> WaitFor(TKey type, int millisecondsTimeout) =>
            WaitFor(type, TimeSpan.FromMilliseconds(millisecondsTimeout));

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<object> WaitFor(TKey key, TimeSpan timeout)
        {
            Task<object> waitTask = WaitFor(key);
            Task timeoutTask = Task.Delay(timeout);
            Task result = await Task.WhenAny(waitTask, timeoutTask);
            if (result == timeoutTask)
            {
                RemoveWaitingTask(key);
                return default;
            }
            return await waitTask;
        }


        internal void RemoveWaitingTask(TKey key)
        {
            AwaitingRequests.Remove(key);
        }

        internal void OnPacket(TKey key, object obj)
        {
            if (AwaitingRequests.TryGetValue(key, out var req))
            {
                req.SetResult(obj);
                RemoveWaitingTask(key);
            }
        }

        bool _disposed = false;
        public void Clear()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var req in AwaitingRequests)
                req.Value.SetResult(default);
            AwaitingRequests.Clear();
        }
    }
}
