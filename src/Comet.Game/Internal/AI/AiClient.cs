using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Comet.Network;
using Comet.Network.Security;
using Comet.Network.Sockets;

namespace Comet.Game.Internal.AI
{
    public sealed class AiClient : TcpServerActor
    {
        /// <summary>
        ///     Instantiates a new instance of <see cref="AiClient" /> using the Accepted event's
        ///     resulting socket and pre-allocated buffer. Initializes all account server
        ///     states, such as the cipher used to decrypt and encrypt data.
        /// </summary>
        /// <param name="socket">Accepted remote client socket</param>
        /// <param name="buffer">pre-allocated buffer from the server listener</param>
        /// <param name="partition">Packet processing partition</param>
        public AiClient(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, null, partition, NetworkDefinition.NPC_FOOTER)
        {
            GUID = Guid.NewGuid().ToString();
        }

        public ConnectionStage Stage { get; set; }
        public string GUID { get; }

        public override Task<int> SendAsync(byte[] packet)
        {
            Kernel.NetworkMonitor.Send(packet.Length);
            return base.SendAsync(packet);
        }

        public enum ConnectionStage
        {
            AwaitingAuth,
            Authenticated
        }
    }
}