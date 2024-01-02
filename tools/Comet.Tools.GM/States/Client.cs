using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Comet.Network;
using Comet.Network.Security;
using Comet.Network.Sockets;

namespace Comet.Tools.GM.States
{
    internal sealed class Client : TcpServerActor
    {
        public DiffieHellman DiffieHellman { get; private set; }

        public Client(Socket socket, Memory<byte> buffer)
            : base(socket, buffer, AesCipher.Create(), 0, NetworkDefinition.GM_TOOLS_FOOTER)
        {
            DiffieHellman = DiffieHellman.Create();
        }

        public override Task<int> SendAsync(byte[] packet)
        {
            Kernel.NetworkMonitor.Send(packet.Length);
            return base.SendAsync(packet);
        }
    }
}
