using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using System.Linq.Expressions;
using System.Threading;

namespace TgenNetProtocol
{
    public partial class FormNetworkBehavour : Form, INetworkObject
    {
        private IEnumerable<MethodInfo> serverMethods;
        private IEnumerable<MethodInfo> clientMethods;
        public IEnumerable<MethodInfo> ServerMethods { get => serverMethods; }
        public IEnumerable<MethodInfo> ClientMethods { get => clientMethods; }

        public FormNetworkBehavour()
        {
            SetUpMethods();

            Thread addToList = new Thread(AddToAttributes);
            addToList.Start();
        }

        public void SetUpMethods()
        {
            Type type = this.GetType();
            serverMethods = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ServerNetworkReciverAttribute), false).FirstOrDefault() != null);
            clientMethods = type.GetMethods().Where(x => x.GetCustomAttributes(typeof(ClientNetworkReciverAttribute), false).FirstOrDefault() != null);
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
                if (!AttributeActions.isWorking)
                {
                    AttributeActions.networkObjects.Add(this);
                    isDone = true;
                }
            }
        }

        private void RemoveFromAttributes()
        {
            bool isDone = false;
            while (!isDone)
            {
                if (!AttributeActions.isWorking)
                {
                    AttributeActions.networkObjects.Remove(this);
                    isDone = true;
                }
            }
        }

        //Hiding isn't intended as it is used for basic dispose, this one is for network dispose
#pragma warning disable CS0108 // 'FormNetworkBehavour.Dispose()' hides inherited member 'Component.Dispose()'. Use the new keyword if hiding was intended.
        public void Dispose()
#pragma warning restore CS0108 // 'FormNetworkBehavour.Dispose()' hides inherited member 'Component.Dispose()'. Use the new keyword if hiding was intended.
        {
            Thread removeFromList = new Thread(RemoveFromAttributes);
            removeFromList.Start();
            base.Dispose(true);
            //Thread removeFromList = new Thread(RemoveFromAttributes);
            //removeFromList.Start(); //the attribute class takes care of null
            //GC.SuppressFinalize(this);
        }

        private void FormNetworkBehavour_Load(object sender, EventArgs e)
        {

        }

        /// <summary>
        /// will not work on static methods
        /// </summary>
        /// <param name="method">The Method to invoke</param>
        /// <param name="objetsToSend">the arguments the Method takes</param>
        /// <param name="ObjectThatOwnsTheMethod">The object that 'owns' the method</param>
        public void InvokeSafely(MethodInfo method, object[] objetsToSend, object ObjectThatOwnsTheMethod)
        {
            if (!method.IsStatic)
            {
                Console.WriteLine("SAFE INVOKE YO");
                var tArgs = new List<Type>();
                foreach (var param in method.GetParameters())
                    tArgs.Add(param.ParameterType);
                tArgs.Add(method.ReturnType);
                var delDecltype = Expression.GetDelegateType(tArgs.ToArray());
                var del = Delegate.CreateDelegate(delDecltype, ObjectThatOwnsTheMethod, method);
                Invoke(del, objetsToSend);
            }
            else
            {
                var tArgs = new List<Type>();
                foreach (var param in method.GetParameters())
                    tArgs.Add(param.ParameterType);
                tArgs.Add(method.ReturnType);
                var delDecltype = Expression.GetDelegateType(tArgs.ToArray());
                var del = Delegate.CreateDelegate(delDecltype, method);
                Invoke(del, objetsToSend);
            }
        }
    }
}
