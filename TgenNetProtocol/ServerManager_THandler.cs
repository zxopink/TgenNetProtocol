using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class ServerManager //TYPE HANDLER
    {
        private Dictionary<(Type type, ClientInfo sender), List<TaskCompletionSource<object>>> AwaitingRequests { get; set; } =
            new Dictionary<(Type type, ClientInfo sender), List<TaskCompletionSource<object>>>();

        /// <summary>Waits for the client to send the type</summary>
        /// <typeparam name="T">The awaiting type</typeparam>
        /// <param name="sender">The client to set the task for</param>
        /// <returns>A task that waits for the client to send the specified type</returns>
        public async Task<T> WaitFor<T>(ClientInfo sender) => (T)await WaitFor(typeof(T), sender);

        public Task<object> WaitFor(Type type, ClientInfo sender)
        {
            var taskSource = new TaskCompletionSource<object>();
            if (AwaitingRequests.TryGetValue((type, sender), out var reqs))
                reqs.Add(taskSource);
            else
            {
                var reqsList = new List<TaskCompletionSource<object>>();
                reqsList.Add(taskSource);
                AwaitingRequests.Add((type, sender), reqsList);
            }
            return taskSource.Task;
        }

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<T> WaitFor<T>(ClientInfo client, int millisecondsTimeout)
        {
            Task<T> waitTask = WaitFor<T>(client);
            Task timeout = Task.Delay(millisecondsTimeout);
            Task result = await Task.WhenAny(waitTask, timeout);
            if (result == timeout)
            {
                RemoveWaitingTask(typeof(T), client, waitTask);
                return default;
            }
            return await waitTask;
        }

        /// <returns>The value or default if value wasn't returned within the set timeout</returns>
        public async Task<object> WaitFor(Type type, ClientInfo client, int millisecondsTimeout)
        {
            Task<object> waitTask = WaitFor(type, client);
            Task timeout = Task.Delay(millisecondsTimeout);
            Task result = await Task.WhenAny(waitTask, timeout);
            if (result == timeout)
            {
                RemoveWaitingTask(type, client, waitTask);
                return default;
            }
            return await waitTask;
        }
        private void RemoveWaitingTask(Type type, ClientInfo client, Task task)
        {
            var key = (type, client);
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

        /////////////////////////////////////////////Registered Methods//////////////////////////////////////////
        
        private Dictionary<Type, List<MethodData>> RegisteredMethods { get; set; } =
            new Dictionary<Type, List<MethodData>>();

        public void Register<T>(Action<T, ClientInfo> method) => Register((Delegate)method);
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

        public void Unregister<T>(Action<T, ClientInfo> method) => Unregister((Delegate)method);
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

        private void CallRegisters(object message, ClientInfo client)
        {
            Type t = message.GetType();
            if (RegisteredMethods.TryGetValue(t, out var list))
            {
                for (int i = 0; i < list.Count; i++)
                {
                    MethodData meth = list[i];
                    if (meth.HasClientData)
                        meth.Invoke(message, client);
                    else
                        meth.Invoke(message);
                }
            }
        }
    }
}
