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
        public static readonly Dictionary<Type/*Object Type*/,
            Dictionary<Type /*Attribute type*/, List<MethodData> /*Methods of object*/>> TotalNetMethods = new Dictionary<Type, Dictionary<Type, List<MethodData>>>();

        protected Dictionary<Type /*Attriute type*/, List<MethodData> /*Methods*/> NetworkMethods = new Dictionary<Type, List<MethodData>>()
        {
                { typeof(ServerReceiverAttribute), new List<MethodData>() },
                { typeof(ClientReceiverAttribute), new List<MethodData>() },
                { typeof(DgramReceiverAttribute), new List<MethodData>() }
        };

        public List<MethodData> ServerMethods => NetworkMethods[typeof(ServerReceiverAttribute)];
        public List<MethodData> ClientMethods => NetworkMethods[typeof(ClientReceiverAttribute)];
        public List<MethodData> DgramMethods => NetworkMethods[typeof(DgramReceiverAttribute)];

        /// <summary>The Netmanagers this instance listens to.
        /// if not set, listens to all active Netmanagers</summary>
        public INetManager[] NetManagers { get; private set; } = Array.Empty<INetManager>();

        public FormNetworkBehavour()
        {
            SetUpMethods();
            this.HandleCreated += FormReady;
        }

        private void FormReady(object sender, EventArgs e) =>
            AddToAttributes();

        public FormNetworkBehavour(params INetManager[] Managers) : this()
        {
            NetManagers = Managers;
        }

        public virtual void SetUpMethods()
        {
            Type thisType = GetType();
            if (!TotalNetMethods.TryGetValue(thisType, out var typeMethods))
            {
                SetTypeMethods();
                return;
            }
            foreach (var attribute in typeMethods.Keys)
            {
                List<MethodData> newTargetedMethods = typeMethods[attribute].Select(method => method.ChangeTarget(this)).ToList();
                NetworkMethods[attribute] = newTargetedMethods;
            }
        }

        protected virtual void SetTypeMethods()
        {
            Type thisType = GetType();
            //Gets public/private/(public inherited) methods
            MethodInfo[] methods = thisType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy | BindingFlags.InvokeMethod);

            foreach (var attribute in NetworkMethods.Keys)
            {
                var actions = methods.Where(x => x.GetCustomAttributes(attribute, false).FirstOrDefault() != null);
                var actionsData = GetMethodsData(actions);
                NetworkMethods[attribute] = actionsData;
            }

            TotalNetMethods.Add(thisType, NetworkMethods);
        }

        private List<MethodData> GetMethodsData(IEnumerable<MethodInfo> methods)
        {
            List<MethodData> methodsData = new List<MethodData>();
            foreach (MethodInfo item in methods)
                methodsData.Add(new MethodData(item, this));

            return methodsData;
        }

        protected virtual void AddToAttributes()
        {
            TypeSetter.Add(this);
        }

        protected virtual void RemoveFromAttributes()
        {
            TypeSetter.Remove(this);
            NetworkMethods.Clear();
        }

        /// <summary>
        /// Removes the object's methods from the network calls
        /// </summary>
        public new virtual void Dispose()
        {
            //Task.Run(RemoveFromAttributes);
            RemoveFromAttributes();
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
            Invoke(method.Method, objetsToSend);
    }
}