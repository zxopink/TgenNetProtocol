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
            if(server == null) throw new NullReferenceException($"{nameof(RUdpClient.FirstPeer)} is null");

            return RequestNatPunch(server.EndPoint, additionalInfo);
        }

        public async Task<NetPeer> RequestNatPunch(IPEndPoint masterServerEndPoint, string additionalInfo = default)
        {
            TaskCompletionSource<NetPeer> intenrnalPeerTask = new TaskCompletionSource<NetPeer>();
            TaskCompletionSource<NetPeer> externalPeerTask = new TaskCompletionSource<NetPeer>();
            EventBasedNatPunchListener natPunchListener = new EventBasedNatPunchListener();
            natPunchListener.NatIntroductionSuccess += (targetEndPoint, type, token) =>
            {
                Console.WriteLine(targetEndPoint + " has connected to " + this.LocalEP + " Connection: " + type);
                OnNatIntroductionSuccess?.Invoke(targetEndPoint, type, token);
                NetPeer partner = Connect(targetEndPoint, ConnectionKey);

                if (type == NatAddressType.Internal)
                    intenrnalPeerTask.SetResult(partner);
                else
                    externalPeerTask.SetResult(partner);
            };
            NatPunchEnabled = true;
            NatPunchModule.Init(natPunchListener);
            NatPunchModule.SendNatIntroduceRequest(masterServerEndPoint, additionalInfo ?? string.Empty);

            var peers = await Task.WhenAll(intenrnalPeerTask.Task, externalPeerTask.Task);
            while (peers[0].ConnectionState == ConnectionState.Outgoing && peers[1].ConnectionState == ConnectionState.Outgoing)
                await Task.Delay(20);
            return peers[0].ConnectionState == ConnectionState.Connected ? peers[0] : peers[1];
        }

        /// <summary>Used by the server side to manage Nat punch</summary>
        /// <returns>A NatMediator instance</returns>
        public NatMediator NatMediator => GetNatMediator();

        private NatMediator _natMediator = default; //Singleton
        private NatMediator GetNatMediator() =>
            _natMediator = _natMediator ?? new NatMediator(RUdpClient);
    }
}
