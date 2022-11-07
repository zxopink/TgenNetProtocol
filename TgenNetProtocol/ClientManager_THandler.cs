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

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<T> WaitFor<T>(int millisecondsTimeout)
        {
            Task<T> waitTask = WaitFor<T>();
            Task timeout = Task.Delay(millisecondsTimeout);
            Task result = Task.WhenAny(waitTask, timeout);
            return result == timeout ? default : await waitTask;
        }

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<object> WaitFor(Type type, int millisecondsTimeout)
        {
            Task<object> waitTask = WaitFor(type);
            Task timeout = Task.Delay(millisecondsTimeout);
            Task result = Task.WhenAny(waitTask, timeout);
            return result == timeout ? default : await waitTask;
        }

        private void OnPacket(object obj)
        {
            Type t = obj.GetType();
            if (AwaitingRequests.TryGetValue(t, out var requests))
            {
                foreach (var req in requests)
                    req.SetResult(obj);
                AwaitingRequests.Remove(t);
            }
        }

        //////////////////////////////////////Registered Methods/////////////////////////////////////////////////////
        private Dictionary<Type, List<MethodData>> RegisteredMethods { get; set; } =
            new Dictionary<Type, List<MethodData>>();

        public void Register<T>(Action<T> method) => Register(method);
        private void Register(Delegate meth)
        {
            MethodData data = (MethodData)meth;
            if (RegisteredMethods.TryGetValue(data.ParameterType, out var methods))
            {
                methods.Add(data);
            }
            else
            {
                List<MethodData> list = new List<MethodData>();
                list.Add(data);
                RegisteredMethods.Add(data.ParameterType, list);
            }
        }

        private void CallRegisters(object message)
        {
            Type t = message.GetType();
            if (RegisteredMethods.TryGetValue(t, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    MethodData meth = list[i];
                    meth.Invoke(message);
                }
            }
        }
    }
}
