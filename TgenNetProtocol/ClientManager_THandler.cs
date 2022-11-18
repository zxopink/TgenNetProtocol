using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class ClientManager
    {
        private Dictionary<Type, List<TaskCompletionSource<object>>> AwaitingRequests { get; set; } =
            new Dictionary<Type, List<TaskCompletionSource<object>>>();

        public async Task<T> WaitFor<T>() => (T)await WaitFor(typeof(T));

        public Task<object> WaitFor(Type type)
        {
            var taskSource = new TaskCompletionSource<object>();
            if (AwaitingRequests.TryGetValue(type, out var reqs))
                reqs.Add(taskSource);
            else
            {
                var reqsStack = new List<TaskCompletionSource<object>>();
                reqsStack.Add(taskSource);
                AwaitingRequests.Add(type, reqsStack);
            }
            return taskSource.Task;
        }

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public Task<T> WaitFor<T>(int millisecondsTimeout) =>
            WaitFor<T>(TimeSpan.FromMilliseconds(millisecondsTimeout));

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<T> WaitFor<T>(TimeSpan timeout)
        {
            Task<T> waitTask = WaitFor<T>();
            Task timeoutTask = Task.Delay(timeout);
            Task result = await Task.WhenAny(waitTask, timeoutTask);
            if (result == timeoutTask)
            {
                RemoveWaitingTask(typeof(T), waitTask);
                return default;
            }
            return await waitTask;
        }

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public Task<object> WaitFor(Type type, int millisecondsTimeout) =>
            WaitFor(type, TimeSpan.FromMilliseconds(millisecondsTimeout));

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<object> WaitFor(Type type, TimeSpan timeout)
        {
            Task<object> waitTask = WaitFor(type);
            Task timeoutTask = Task.Delay(timeout);
            Task result = await Task.WhenAny(waitTask, timeoutTask);
            if (result == timeoutTask)
            {
                RemoveWaitingTask(type, waitTask);
                return default;
            }
            return await waitTask;
        }


        private void RemoveWaitingTask(Type type, Task task)
        {
            if (AwaitingRequests.TryGetValue(type, out var reqs))
            {
                for (int i = 0; i < reqs.Count; i++)
                {
                    if (task == reqs[i].Task)
                        reqs.RemoveAt(i);
                    if (reqs.Count == 0)
                        AwaitingRequests.Remove(type);
                }
            }
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

        public void Register<T>(Action<T> method) => Register((Delegate)method);
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

        public void Unregister<T>(Action<T> method) => Unregister((Delegate)method);
        private bool Unregister(Delegate meth)
        {
            MethodData data = (MethodData)meth;
            Type type = data.ParameterType;
            if (RegisteredMethods.TryGetValue(type, out var methods))
                for (int i = 0; i < methods.Count; i++)
                    if (methods[i] == data)
                    {
                        methods.RemoveAt(i);
                        if (methods.Count == 0)
                            RegisteredMethods.Remove(type);
                        return true;
                    }

            return false;
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
