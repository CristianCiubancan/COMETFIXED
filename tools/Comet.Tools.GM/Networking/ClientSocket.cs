using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Comet.Network;
using Comet.Network.Packets;
using Comet.Network.Sockets;
using Comet.Shared;
using Comet.Tools.GM.Networking.Packets;
using Comet.Tools.GM.States;

namespace Comet.Tools.GM.Networking
{
    internal sealed class ClientSocket : TcpClientWrapper<Client>
    {
        private readonly PacketProcessor<Client> mProcessor;

        public ClientSocket()
            : base(NetworkDefinition.GM_TOOLS_FOOTER.Length, false, true)
        {
            mProcessor = new PacketProcessor<Client>(ProcessAsync);
            _ = mProcessor.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task<Client> ConnectedAsync(Socket socket, Memory<byte> buffer)
        {
            Client client = new(socket, buffer);
            return socket.Connected ? client : null;
        }

        /// <inheritdoc />
        protected override async Task<bool> ExchangedAsync(Client actor, Memory<byte> buffer)
        {
            try
            {
                MsgUpdHandshake handshake = new MsgUpdHandshake();
                handshake.Decode(buffer.ToArray());
                await handshake.ProcessAsync(actor);
                return true;
            }
            catch
            {
                await Log.WriteLogAsync(LogLevel.Socket, "Exchange failed!!!");
                return false;
            }
        }

        protected override void Received(Client actor, ReadOnlySpan<byte> packet)
        {
            Kernel.NetworkMonitor.Receive(packet.Length);
            mProcessor.Queue(actor, packet.ToArray());
        }

        private async Task ProcessAsync(Client actor, byte[] packet)
        {
            // Validate connection
            if (!actor.Socket.Connected)
                return;

            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType)BitConverter.ToUInt16(packet, 2);

            try
            {
                MsgBase<Client> msg = null;
                switch (type)
                {

                    default:
                        await Log.WriteLogAsync(LogLevel.Warning,
                                                "Missing packet {0}, Length {1}\n{2}",
                                                type, length, PacketDump.Hex(packet));
                        return;
                }

                // Decode packet bytes into the structure and process
                msg.Decode(packet);
                await msg.ProcessAsync(actor);
            }
            catch (Exception e)
            {
                await Log.WriteLogAsync(LogLevel.Exception, e.Message);
            }
        }

        protected override void Disconnected(Client actor)
        {
            Log.WriteLogAsync(LogLevel.Info, "Disconnected from the account server!").ConfigureAwait(false);
            
        }
    }
}
