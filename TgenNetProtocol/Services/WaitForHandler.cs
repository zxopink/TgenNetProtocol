using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    internal class WaitForHandler<TKey>
    {
        public Dictionary<TKey, List<TaskCompletionSource<object>>> AwaitingRequests { get; private set; }

        public WaitForHandler()
        {
            AwaitingRequests = new Dictionary<TKey, List<TaskCompletionSource<object>>>();
        }

        public Task<object> WaitFor(TKey key)
        {
            var taskSource = new TaskCompletionSource<object>();
            if (AwaitingRequests.TryGetValue(key, out var reqs))
                reqs.Add(taskSource);
            else
            {
                var reqsStack = new List<TaskCompletionSource<object>>();
                reqsStack.Add(taskSource);
                AwaitingRequests.Add(key, reqsStack);
            }
            return taskSource.Task;
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
                RemoveWaitingTask(key, waitTask);
                return default;
            }
            return await waitTask;
        }


        internal void RemoveWaitingTask(TKey key, Task task)
        {
            if (AwaitingRequests.TryGetValue(key, out var reqs))
            {
                for (int i = 0; i < reqs.Count; i++)
                {
                    if (task == reqs[i].Task)
                        reqs.RemoveAt(i);
                    if (reqs.Count == 0)
                        AwaitingRequests.Remove(key);
                }
            }
        }

        internal void OnPacket(TKey key, object obj)
        {
            Type t = obj.GetType();
            if (AwaitingRequests.TryGetValue(key, out var requests))
            {
                foreach (var req in requests)
                    req.SetResult(obj);
                AwaitingRequests.Remove(key);
            }
        }

        bool _disposed = false;
        public void Clear()
        {
            if (_disposed) return;
            _disposed = true;

            foreach (var req in AwaitingRequests)
            {
                foreach (var taskCompl in req.Value)
                    taskCompl.SetResult(default); //Complete all tasks

                req.Value.Clear();
            }
            AwaitingRequests.Clear();
        }
    }
}
