using System.Net.Sockets;
using Comet.Launcher.Packets;
using Comet.Launcher.States;
using Comet.Launcher.Threads;
using Comet.Network;
using Comet.Network.Packets;
using Comet.Network.Sockets;
using Comet.Shared;

namespace Comet.Launcher
{
    public sealed class Client : TcpClientWrapper<Server>
    {
        private readonly LauncherThread mThread;
        private readonly PacketProcessor<Server> mProcessor;

        public Client(LauncherThread thread)
            : base(NetworkDefinition.PATCHER_FOOTER.Length, false, true)
        {
            mThread = thread;
            mProcessor = new PacketProcessor<Server>(ProcessAsync, 1);
            mProcessor.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        protected override async Task<Server> ConnectedAsync(Socket socket, Memory<byte> buffer)
        {
            Server client = new(socket, buffer, mProcessor.SelectPartition());
            if (socket.Connected)
            {
                await mThread.OnConnectAsync(client);
                return client;
            }
            return null;
        }

        /// <inheritdoc />
        protected override async Task<bool> ExchangedAsync(Server actor, Memory<byte> buffer)
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

        protected override void Received(Server actor, ReadOnlySpan<byte> packet)
        {
            mProcessor.Queue(actor, packet.ToArray());
        }

        private async Task ProcessAsync(Server actor, byte[] packet)
        {
            // Validate connection
            if (!actor.Socket.Connected)
                return;

            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType) BitConverter.ToUInt16(packet, 2);

            try
            {
                MsgBase<Server> msg = null;
                switch (type)
                {
                    case PacketType.MsgUpdLoginEx:
                    {
                        msg = new MsgUpdLoginEx();
                        break;
                    }

                    case PacketType.MsgUpdPatchList:
                    {
                        msg = new MsgUpdPatchList();
                        break;
                    }

                    case PacketType.MsgUpdConfirmHash:
                    {
                        msg = new MsgUpdConfirmHash();
                        break;
                    }

                    case PacketType.MsgUpdPing:
                    {
                        msg = new MsgUpdPing();
                        break;
                    }

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

        protected override void Disconnected(Server actor)
        {
            _ = mThread.OnDisconnectAsync().ConfigureAwait(true);
        }
    }
}
