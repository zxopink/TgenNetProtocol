using System;

namespace TgenNetProtocol
{
    #region Attributes
    /// <summary>
    /// Attribute for methods that receive packets from the ServerManager protocol
    /// Can also be given a ClientData parameter to get data over connected users (clients)
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ServerReceiverAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for methods that receive packets from the ClientManager protocol
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class ClientReceiverAttribute : Attribute
    {
    }

    /// <summary>
    /// Attribute for methods that receive packets from the UDP protocol
    /// Can also be given a [TODO] parameter to get data over the EndPoint who sent packet
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class DgramReceiverAttribute : Attribute
    {
    }
    #endregion
}