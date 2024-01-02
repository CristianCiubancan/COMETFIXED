using System.Net.Sockets;
using Comet.Network;
using Comet.Network.Security;
using Comet.Network.Sockets;

namespace Comet.Launcher.States
{
    public sealed class Server : TcpServerActor
    {
        public DiffieHellman DiffieHellman { get; }

        /// <summary>
        ///     Instantiates a new instance of <see cref="Client" /> using the Accepted event's
        ///     resulting socket and pre-allocated buffer. Initializes all account server
        ///     states, such as the cipher used to decrypt and encrypt data.
        /// </summary>
        /// <param name="socket">Accepted remote client socket</param>
        /// <param name="buffer">Pre-allocated buffer from the server listener</param>
        /// <param name="partition">Packet processing partition</param>
        public Server(Socket socket, Memory<byte> buffer, uint partition)
            : base(socket, buffer, AesCipher.Create(), partition, NetworkDefinition.PATCHER_FOOTER)
        {
            DiffieHellman = DiffieHellman.Create();
        }
    }
}
