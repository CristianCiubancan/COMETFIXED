using System;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Comet.Account.Database;
using Comet.Account.Packets;
using Comet.Account.States;
using Comet.Database.Entities;
using Comet.Network;
using Comet.Network.Packets;
using Comet.Network.Sockets;
using Comet.Shared;

namespace Comet.Account
{
    internal sealed class IntraServer : TcpServerListener<GameServer>
    {
        // Fields and Properties
        private readonly PacketProcessor<GameServer> mProcessor;

        public IntraServer(ServerConfiguration config)
            : base(100, 8192, false, false, NetworkDefinition.ACCOUNT_FOOTER.Length)
        {
            mProcessor = new PacketProcessor<GameServer>(ProcessAsync);
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
        protected override async Task<GameServer> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            uint partition = mProcessor.SelectPartition();
            var client = new GameServer(socket, buffer, partition);
            await Log.WriteLogAsync(LogLevel.Info, $"Accepting connection from server on [{client.IpAddress}].");
            return client;
        }

        /// <summary>
        ///     Invoked by the server listener's Receiving method to process a completed packet
        ///     from the actor's socket pipe. At this point, the packet has been assembled and
        ///     split off from the rest of the buffer.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="packet">Packet bytes to be processed</param>
        protected override void Received(GameServer actor, ReadOnlySpan<byte> packet)
        {
            mProcessor.Queue(actor, packet.ToArray());
        }

        /// <summary>
        ///     Invoked by one of the server's packet processor worker threads to process a
        ///     single packet of work. Allows the server to process packets as individual
        ///     messages on a single channel.
        /// </summary>
        /// <param name="actor">Actor requesting packet processing</param>
        /// <param name="packet">An individual data packet to be processed</param>
        private async Task ProcessAsync(GameServer actor, byte[] packet)
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
                MsgBase<GameServer> msg = null;
                switch (type)
                {
                    case PacketType.MsgAccServerExchange:
                        msg = new MsgAccServerExchange();
                        break;

                    case PacketType.MsgAccServerLoginExchangeEx:
                        msg = new MsgAccServerLoginExchangeEx();
                        break;

                    case PacketType.MsgAccServerPlayerExchange:
                        msg = new MsgAccServerPlayerExchange();
                        break;

                    case PacketType.MsgAccServerPlayerStatus:
                        msg = new MsgAccServerPlayerStatus();
                        break;

                    case PacketType.MsgAccServerGameInformation:
                        msg = new MsgAccServerGameInformation();
                        break;

                    default:
                        Console.WriteLine("Missing packet {0}, Length {1}\n{2}",
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

        protected override void Disconnected(GameServer actor)
        {
            if (actor.Realm == null)
                return;

            _ = Log.WriteLogAsync(LogLevel.Info, $"Server [{actor.Realm.Name}] has disconnected.")
                   .ConfigureAwait(false);

            try
            {
                actor.Realm.Status = DbRealm.RealmStatus.Offline;
                actor.Realm.LastPing = DateTime.Now;
                _ = ServerDbContext.SaveAsync(actor.Realm).ConfigureAwait(false);
            }
            catch
            {
            }

            // cleanup server data?
            foreach (Player player in Kernel.Players.Values.Where(x => x.Realm.RealmID == actor.Realm.RealmID))
                Kernel.Players.TryRemove(player.AccountIdentity, out _);

            actor.Realm.Server = null;
        }
    }
}