using System;

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