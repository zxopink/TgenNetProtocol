using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Reflection;

namespace TgenNetProtocol.WinForms
{
    public partial class FormNetworkBehavour : Form, INetworkObject
    {
        public List<MethodData> ServerMethods { get; private set; }
        public List<MethodData> ClientMethods { get; private set; }
        public List<MethodData> DgramMethods { get; private set; }

        public FormNetworkBehavour()
        {
            SetUpMethods();
            this.HandleCreated += FormReady;
        }

        private void FormReady(object sender, EventArgs e) =>
            Add2Attributes();//Task.Run(AddToAttributes);

        public void SetUpMethods()
        {
            MethodInfo[] methods = GetType().GetMethods();
            IEnumerable<MethodInfo> serverActions = methods.Where(x => x.GetCustomAttributes(typeof(ServerReceiverAttribute), false).FirstOrDefault() != null);
            IEnumerable<MethodInfo> clientActions = methods.Where(x => x.GetCustomAttributes(typeof(ClientReceiverAttribute), false).FirstOrDefault() != null);
            IEnumerable<MethodInfo> dgramAction = methods.Where(x => x.GetCustomAttributes(typeof(DgramReceiverAttribute), false).FirstOrDefault() != null);

            ServerMethods = GetMethodsData(serverActions);
            ClientMethods = GetMethodsData(clientActions);
            DgramMethods = GetMethodsData(dgramAction);
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
        private void Add2Attributes()
        {
            TgenLog.Log("adding " + this.ToString() + " to the list");
            TypeSetter.networkObjects.Add(this);
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
                DgramMethods.Clear();
            }
        }

        private void Remove2Attributes()
        {
            int index = TypeSetter.networkObjects.IndexOf(this);
            if (index != -1) //Found
                TypeSetter.networkObjects[index] = null;

            ServerMethods.Clear();
            ClientMethods.Clear();
            DgramMethods.Clear();
        }

#pragma warning disable CS0108 // 'FormNetworkBehavour.Dispose()' hides inherited member 'Component.Dispose()'. Use the new keyword if hiding was intended.
        //Hiding isn't intended as it is used for basic dispose, this one is for network dispose
        /// <summary>
        /// Removes the object's methods from the network calls
        /// </summary>
        public void Dispose()
        {
            //Task.Run(RemoveFromAttributes);
            Remove2Attributes();
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