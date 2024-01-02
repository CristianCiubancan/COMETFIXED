using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Comet.Game.Packets;
using Comet.Network;
using Comet.Network.Packets;
using Comet.Network.Sockets;
using Comet.Shared;
using static Comet.Game.Database.ServerConfiguration;

namespace Comet.Game.Internal.Auth
{
    public sealed class AccountClient : TcpClientWrapper<AccountServer>
    {
        public static RpcNetworkConfiguration Configuration;

        private readonly PacketProcessor<AccountServer> mProcessor;

        public AccountClient()
            : base(NetworkDefinition.ACCOUNT_FOOTER.Length)
        {
            mProcessor = new PacketProcessor<AccountServer>(ProcessAsync);
            _ = mProcessor.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task<AccountServer> ConnectedAsync(Socket socket, Memory<byte> buffer)
        {
            AccountServer client = new(socket, buffer, 0);
            if (socket.Connected)
            {
                Kernel.AccountServer = client;
                Kernel.AccountClient = this;

                await client.SendAsync(new MsgAccServerExchange
                {
                    ServerName = Kernel.GameConfiguration.ServerName,
                    Username = Kernel.GameConfiguration.Username,
                    Password = Kernel.GameConfiguration.Password
                });
                return client;
            }

            return null;
        }

        protected override void Received(AccountServer actor, ReadOnlySpan<byte> packet)
        {
            Kernel.NetworkMonitor.Receive(packet.Length);
            mProcessor.Queue(actor, packet.ToArray());
        }

        private async Task ProcessAsync(AccountServer actor, byte[] packet)
        {
            // Validate connection
            if (!actor.Socket.Connected)
                return;

            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType) BitConverter.ToUInt16(packet, 2);

            try
            {
                MsgBase<AccountServer> msg = null;
                switch (type)
                {
                    case PacketType.MsgPCNum:
                        msg = new MsgPCNum();
                        break;

                    case PacketType.MsgAccServerAction:
                        msg = new MsgAccServerAction();
                        break;

                    case PacketType.MsgAccServerLoginExchange:
                        msg = new MsgAccServerLoginExchange();
                        break;

                    case PacketType.MsgAccServerCmd:
                        msg = new MsgAccServerCmd();
                        break;

                    case PacketType.MsgAccServerPing:
                        msg = new MsgAccServerPing();
                        break;

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

        protected override void Disconnected(AccountServer actor)
        {
            Log.WriteLogAsync(LogLevel.Info, "Disconnected from the account server!").ConfigureAwait(false);

            Kernel.AccountClient = null;
            Kernel.AccountServer = null;
        }
    }
}