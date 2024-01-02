using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Comet.Network;
using Comet.Network.Security;
using Comet.Network.Sockets;

namespace Comet.Game.Internal.Auth
{
    public sealed class AccountServer : TcpServerActor
    {
        /// <summary>
        ///     Instantiates a new instance of <see cref="AccountClient" /> using the Accepted event's
        ///     resulting socket and pre-allocated buffer. Initializes all account server
        ///     states, such as the cipher used to decrypt and encrypt data.
        /// </summary>
        /// <param name="socket">Accepted remote client socket</param>
        /// <param name="buffer">Pre-allocated buffer from the server listener</param>
        /// <param name="partition">Packet processing partition</param>
        public AccountServer(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, AesCipher.Create(), partition, NetworkDefinition.ACCOUNT_FOOTER)
        {
        }

        public override Task<int> SendAsync(byte[] packet)
        {
            Kernel.NetworkMonitor.Send(packet.Length);
            return base.SendAsync(packet);
        }
    }
}