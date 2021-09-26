using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class FormNetworkBehavour : Form, INetworkObject
    {
        private List<MethodData> serverMethods;
        private List<MethodData> clientMethods;
        public List<MethodData> ServerMethods { get => serverMethods; }
        public List<MethodData> ClientMethods { get => clientMethods; }

        public FormNetworkBehavour()
        {
            SetUpMethods();

            this.HandleCreated += FormReady;
        }

        private void FormReady(object sender, EventArgs e) =>
            Task.Run(AddToAttributes);

        public void SetUpMethods()
        {
            Type type = GetType();
            IEnumerable<MethodInfo> serverActions = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ServerReceiverAttribute), false).FirstOrDefault() != null);
            IEnumerable<MethodInfo> clientActions = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ClientReceiverAttribute), false).FirstOrDefault() != null);

            serverMethods = GetMethodsData(serverActions);
            clientMethods = GetMethodsData(clientActions);
        }

        private List<MethodData> GetMethodsData(IEnumerable<MethodInfo> methods)
        {
            List<MethodData> methodDatas = new List<MethodData>();
            foreach (MethodInfo item in methods)
                methodDatas.Add(new MethodData(item, this));

            return methodDatas;
        }

        /// <summary>
        /// This method makes sure the other threads that sends message isn't getting effected while it's active
        /// Things can break if two thread work on the same variable/method
        /// </summary>
        private void AddToAttributes()
        {
            bool isDone = false;
            while (!isDone)
            {
                if (!TypeSetter.isWorking)
                {
                    TypeSetter.networkObjects.Add(this);
                    isDone = true;
                }
            }
        }

        private void RemoveFromAttributes()
        {
            bool isDone = false;
            while (!isDone)
            {
                if (!TypeSetter.isWorking)
                {
                    TypeSetter.networkObjects.Remove(this);
                    isDone = true;
                }
                ServerMethods.Clear();
                ClientMethods.Clear();
            }
        }

#pragma warning disable CS0108 // 'FormNetworkBehavour.Dispose()' hides inherited member 'Component.Dispose()'. Use the new keyword if hiding was intended.
        //Hiding isn't intended as it is used for basic dispose, this one is for network dispose
        /// <summary>
        /// Removes the object's methods from the network calls
        /// </summary>
        public void Dispose()
        {
            Task.Run(RemoveFromAttributes);
            base.Dispose(true);
        }

        private void FormNetworkBehavour_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="method">The Method to invoke</param>
        /// <param name="objetsToSend">the arguments the Method takes</param>
        public void InvokeNetworkMethods(MethodData method, object[] objetsToSend) =>
            Invoke(method.Delegate, objetsToSend);
    }
}
