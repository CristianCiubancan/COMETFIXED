using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Comet.Game.Packets.Ai;
using Comet.Network;
using Comet.Network.Packets;
using Comet.Network.Sockets;
using Comet.Shared;
using static Comet.Network.Packets.Ai.MsgAiAction<Comet.Game.Internal.AI.AiClient>;

namespace Comet.Game.Internal.AI
{
    public sealed class AiServer : TcpServerListener<AiClient>
    {
        // Fields and Properties
        private readonly PacketProcessor<AiClient> mProcessor;

        /// <summary>
        ///     Instantiates a new instance of <see cref="Server" /> by initializing the
        ///     <see cref="PacketProcessor{TClient}" /> for processing packets from the players using
        ///     channels and worker threads. Initializes the TCP server listener.
        /// </summary>
        public AiServer()
            : base(1, exchange: false, footerLength: NetworkDefinition.NPC_FOOTER.Length)
        {
            mProcessor = new PacketProcessor<AiClient>(ProcessAsync, 1);
            _ = mProcessor.StartAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        ///     Invoked by the server listener's Accepting method to create a new server actor
        ///     around the accepted client socket. Gives the server an opportunity to initialize
        ///     any processing mechanisms or authentication routines for the client connection.
        /// </summary>
        /// <param name="socket">Accepted client socket from the server socket</param>
        /// <param name="buffer">pre-allocated buffer from the server listener</param>
        /// <returns>A new instance of a ServerActor around the client socket</returns>
        protected override async Task<AiClient> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            uint partition = mProcessor.SelectPartition();
            var client = new AiClient(socket, buffer, partition);

            await client.SendAsync(new MsgAiAction
            {
                Action = AiAction.RequestLogin
            });
            return client;
        }

        /// <summary>
        ///     Invoked by the server listener's Receiving method to process a completed packet
        ///     from the actor's socket pipe. At this point, the packet has been assembled and
        ///     split off from the rest of the buffer.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="packet">Packet bytes to be processed</param>
        protected override void Received(AiClient actor, ReadOnlySpan<byte> packet)
        {
            Kernel.NetworkMonitor.Receive(packet.Length);
            mProcessor.Queue(actor, packet.ToArray());
        }

        /// <summary>
        ///     Invoked by one of the server's packet processor worker threads to process a
        ///     single packet of work. Allows the server to process packets as individual
        ///     messages on a single channel.
        /// </summary>
        /// <param name="actor">Actor requesting packet processing</param>
        /// <param name="packet">An individual data packet to be processed</param>
        private async Task ProcessAsync(AiClient actor, byte[] packet)
        {
            // Validate connection
            if (!actor.Socket.Connected)
                return;

            // Read in TQ's binary header
            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType) BitConverter.ToUInt16(packet, 2);

            try
            {
                // Switch on the packet type
                MsgBase<AiClient> msg = null;
                switch (type)
                {
                    case PacketType.MsgAiLoginExchange:
                    {
                        msg = new MsgAiLoginExchange();
                        break;
                    }

                    case PacketType.MsgAiSpawnNpc:
                    {
                        msg = new MsgAiSpawnNpc();
                        break;
                    }

                    case PacketType.MsgAiPing:
                    {
                        msg = new MsgAiPing();
                        break;
                    }

                    case PacketType.MsgAiRoleLogin:
                    {
                        msg = new MsgAiRoleLogin();
                        break;
                    }

                    case PacketType.MsgAction:
                    {
                        msg = new MsgAction();
                        break;
                    }

                    case PacketType.MsgInteract:
                    {
                        msg = new MsgInteract();
                        break;
                    }

                    case PacketType.MsgWalk:
                    {
                        msg = new MsgWalk();
                        break;
                    }

                    case PacketType.MsgTalk:
                    {
                        msg = new MsgTalk();
                        break;
                    }

                    default:
                        await Log.WriteLogAsync(LogLevel.Socket, $"Missing packet {type}, Length {length}\n{PacketDump.Hex(packet)}");
                        return;
                }

                // Decode packet bytes into the structure and process
                msg.Decode(packet);
                // Packet has been decrypted and now will be queued in the region processor
                await msg.ProcessAsync(actor);
            }
            catch (Exception e)
            {
                await Log.WriteLogAsync(LogLevel.Socket, $"{e.Message}\r\n{e}");
            }
        }

        /// <summary>
        ///     Invoked by the server listener's Disconnecting method to dispose of the actor's
        ///     resources. Gives the server an opportunity to cleanup references to the actor
        ///     from other actors and server collections.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        protected override void Disconnected(AiClient actor)
        {
            Kernel.AiServer = null;
            _ = Log.WriteLogAsync($"{actor.GUID} [{actor.IpAddress}] AI has disconnected.");
        }
    }
}