using LiteNetLib;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace TgenNetProtocol
{
    public partial class UdpManager
    {
        public delegate void NatIntroductionSuccessDelegate(IPEndPoint targetEndPoint, NatAddressType type, string token);
        public event NatIntroductionSuccessDelegate OnNatIntroductionSuccess;

        /// <summary>Makes a nat punch request to the first peer (server)</summary>
        /// <exception cref="NullReferenceException">thrown if client has yet to connect another peer</exception>
        public Task<NetPeer> RequestNatPunch(string additionalInfo = default)
        {
            var server = RUdpClient.FirstPeer;
            _ = server ?? throw new NullReferenceException($"{nameof(RUdpClient.FirstPeer)} is null");

            return RequestNatPunch(server.EndPoint, additionalInfo);
        }

        public Task<NetPeer> RequestNatPunch(IPEndPoint masterServerEndPoint, string additionalInfo = default)
        {
            NatPunchEnabled = true;
            TaskCompletionSource<NetPeer> peerTask = new TaskCompletionSource<NetPeer>();
            EventBasedNatPunchListener natPunchListener = new EventBasedNatPunchListener();
            natPunchListener.NatIntroductionSuccess += (targetEndPoint, type, token) =>
            {
                OnNatIntroductionSuccess?.Invoke(targetEndPoint, type, token);
                NetPeer partner = Connect(targetEndPoint, ConnectionKey);
                peerTask.SetResult(partner);
            };
            NatPunchModule.Init(natPunchListener);
            NatPunchModule.SendNatIntroduceRequest(masterServerEndPoint, additionalInfo ?? string.Empty);
            return peerTask.Task;
        }

        /// <summary>Used by the server side to manage Nat punch</summary>
        /// <returns>A NatMediator instance</returns>
        public NatMediator CreateNatMediator()
        {
            return new NatMediator(RUdpClient);
        }
    }
}
