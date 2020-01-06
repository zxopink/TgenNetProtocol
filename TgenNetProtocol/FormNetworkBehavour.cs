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
    public abstract partial class FormNetworkBehavour : Form
    {
        public FormNetworkBehavour()
        {
            Thread addToList = new Thread(AddToAttributes);
            addToList.Start();
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
