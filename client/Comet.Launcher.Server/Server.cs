using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Comet.Launcher.Server.Packets;
using Comet.Launcher.Server.States;
using Comet.Network;
using Comet.Network.Packets;
using Comet.Network.Sockets;
using Comet.Shared;

namespace Comet.Launcher.Server
{
    internal sealed class Server : TcpServerListener<Client>
    {
        private readonly PacketProcessor<Client> mProcessor;

        public Server(ServerConfiguration config) 
            : base(config.Network.MaxConn, 4096, false, true, NetworkDefinition.PATCHER_FOOTER.Length)
        {
            mProcessor = new PacketProcessor<Client>(ProcessAsync);
            _ = mProcessor.StartAsync(CancellationToken.None).ConfigureAwait(false);
            ExchangeStartPosition = 0;
        }

        /// <inheritdoc />
        protected override async Task<Client> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            uint partition = mProcessor.SelectPartition();
            var client = new Client(socket, buffer, partition);
            await Log.WriteLogAsync(LogLevel.Info, $"Accepting connection from client [{client.GUID}] on [{client.IpAddress}].");
            return client;
        }

        /// <inheritdoc />
        protected override async Task<bool> ExchangedAsync(Client actor, Memory<byte> buffer)
        {
            try
            {
                if (actor.DiffieHellman == null)
                    throw new NullReferenceException("DiffieHellman cannot be null");

                MsgUpdHandshake handshake = new MsgUpdHandshake();
                handshake.Decode(buffer.ToArray());
                await handshake.ProcessAsync(actor);

                return true;
            }
            catch
            {
                await Log.WriteLogAsync("Handshake failed!!!");
                return false;
            }
        }

        protected override void Received(Client actor, ReadOnlySpan<byte> packet)
        {
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
                // Switch on the packet type
                MsgBase<Client> msg = null;
                switch (type)
                {
                    case PacketType.MsgUpdLogin:
                    {
                        msg = new MsgUpdLogin();
                        break;
                    }

                    case PacketType.MsgUpdQueryVersion:
                    {
                        msg = new MsgUpdQueryVersion();
                        break;
                    }

                    case PacketType.MsgUpdCheckHash:
                    {
                        msg = new MsgUpdCheckHash();
                        break;
                    }

                    case PacketType.MsgUpdPing:
                    {
                        msg = new MsgUpdPing();
                        break;
                    }

                    default:
                        await Log.WriteLogAsync(LogLevel.Socket, "Missing packet {0}, Length {1}\n{2}",
                                          type, length, PacketDump.Hex(packet));
                        return;
                }

                // Decode packet bytes into the structure and process
                msg.Decode(packet);
                await msg.ProcessAsync(actor);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        protected override void Disconnected(Client actor)
        {
            if (Kernel.Clients.TryRemove(actor.GUID, out _))
            {
                // TODO signal account server to disconnect accounts
            }
        }
    }
}
