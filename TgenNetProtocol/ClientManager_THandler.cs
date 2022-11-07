using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class ClientManager
    {
        private Dictionary<Type, Stack<TaskCompletionSource<object>>> AwaitingRequests { get; set; } =
            new Dictionary<Type, Stack<TaskCompletionSource<object>>>();

        public async Task<T> WaitFor<T>() => (T)await WaitFor(typeof(T));

        public Task<object> WaitFor(Type type)
        {
            var taskSource = new TaskCompletionSource<object>();
            if (AwaitingRequests.TryGetValue(type, out var reqs))
                reqs.Push(taskSource);
            else
            {
                var reqsStack = new Stack<TaskCompletionSource<object>>();
                reqsStack.Push(taskSource);
                AwaitingRequests.Add(type, reqsStack);
            }
            return taskSource.Task;
        }

        private void OnPacket(object obj)
        {
            Type t = obj.GetType();
            if (AwaitingRequests.TryGetValue(t, out var requests))
            {
                foreach (var req in requests)
                    req.SetResult(obj);
            }
        }
    }
}
