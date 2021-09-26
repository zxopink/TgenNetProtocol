using System;

namespace TgenNetProtocol
{
    #region Attributes
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerReceiverAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class ClientReceiverAttribute : Attribute
    {
    }
    #endregion
}