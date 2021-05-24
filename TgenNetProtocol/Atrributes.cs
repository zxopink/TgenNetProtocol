using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    #region Attributes
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerNetworkReciverAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientNetworkReciverAttribute : Attribute
    {
    }
    #endregion
}