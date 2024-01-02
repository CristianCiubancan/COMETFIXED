using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Game.World;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Network.Security;
using Comet.Network.Sockets;
using Comet.Shared;

namespace Comet.Game
{
    /// <summary>
    ///     Server inherits from a base server listener to provide the game server with
    ///     listening functionality and event handling. This class defines how the server
    ///     listener and invoked events are customized for the game server.
    /// </summary>
    internal sealed class Server : TcpServerListener<Client>
    {
        private static Server GameServer;

        // Fields and Properties
        private readonly PacketProcessor<Client> Processor;

        /// <summary>
        ///     Instantiates a new instance of <see cref="Server" /> by initializing the
        ///     <see cref="PacketProcessor" /> for processing packets from the players using
        ///     channels and worker threads. Initializes the TCP server listener.
        /// </summary>
        /// <param name="config">The server's read configuration file</param>
        public Server(ServerConfiguration config)
            : base(config.GameNetwork.MaxConn, exchange: true, footerLength: 8)
        {
            Processor = new PacketProcessor<Client>(ProcessAsync);
            _ = Processor.StartAsync(CancellationToken.None).ConfigureAwait(false);

            GameServer = this;
        }

        /// <summary>
        ///     Invoked by the server listener's Accepting method to create a new server actor
        ///     around the accepted client socket. Gives the server an opportunity to initialize
        ///     any processing mechanisms or authentication routines for the client connection.
        /// </summary>
        /// <param name="socket">Accepted client socket from the server socket</param>
        /// <param name="buffer">pre-allocated buffer from the server listener</param>
        /// <returns>A new instance of a ServerActor around the client socket</returns>
        protected override async Task<Client> AcceptedAsync(Socket socket, Memory<byte> buffer)
        {
            uint partition = Processor.SelectPartition();
            var client = new Client(socket, buffer, partition);
            await client.NdDiffieHellman.ComputePublicKeyAsync();

            await Kernel.NextBytesAsync(client.NdDiffieHellman.DecryptionIV);
            await Kernel.NextBytesAsync(client.NdDiffieHellman.EncryptionIV);

            var handshakeRequest = new MsgHandshake(
                client.NdDiffieHellman,
                client.NdDiffieHellman.EncryptionIV,
                client.NdDiffieHellman.DecryptionIV);

            await handshakeRequest.RandomizeAsync();
            await client.SendAsync(handshakeRequest);
            return client;
        }

        /// <summary>
        ///     Invoked by the server listener's Exchanging method to process the client
        ///     response from the Diffie-Hellman Key Exchange. At this point, the raw buffer
        ///     from the client has been decrypted and is ready for direct processing.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="buffer">Packet buffer to be processed</param>
        /// <returns>True if the exchange was successful.</returns>
        protected override Task<bool> ExchangedAsync(Client actor, Memory<byte> buffer)
        {
            try
            {
                var msg = new MsgHandshake();
                msg.Decode(buffer.ToArray());

                actor.NdDiffieHellman.ComputePrivateKey(msg.ClientKey);

                actor.Cipher.GenerateKeys(new object[]
                {
                    actor.NdDiffieHellman.PrivateKey.ToByteArrayUnsigned()
                });
                (actor.Cipher as BlowfishCipher).SetIVs(
                    actor.NdDiffieHellman.DecryptionIV,
                    actor.NdDiffieHellman.EncryptionIV);

                actor.NdDiffieHellman = null;

                // We are setting a higher timeout just because the user may stay for a while in the login screen.
                // We are doing this here because we don't want the user to hold a connection without exchanging.
                // After the user receive the exchange data it'll be ready to receive new packets, so we will
                // make the wait longer here.
                // We are setting it back if the login is successfull!
                actor.ReceiveTimeOutSeconds = 900;
                return Task.FromResult(true);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return Task.FromResult(false);
        }

        /// <summary>
        ///     Invoked by the server listener's Receiving method to process a completed packet
        ///     from the actor's socket pipe. At this point, the packet has been assembled and
        ///     split off from the rest of the buffer.
        /// </summary>
        /// <param name="actor">Server actor that represents the remote client</param>
        /// <param name="packet">Packet bytes to be processed</param>
        protected override void Received(Client actor, ReadOnlySpan<byte> packet)
        {
            Kernel.NetworkMonitor.Receive(packet.Length);
            Processor.Queue(actor, packet.ToArray());
        }

        /// <summary>
        ///     Invoked by one of the server's packet processor worker threads to process a
        ///     single packet of work. Allows the server to process packets as individual
        ///     messages on a single channel.
        /// </summary>
        /// <param name="actor">Actor requesting packet processing</param>
        /// <param name="packet">An individual data packet to be processed</param>
        private async Task ProcessAsync(Client actor, byte[] packet)
        {
            // Read in TQ's binary header
            var length = BitConverter.ToUInt16(packet, 0);
            var type = (PacketType) BitConverter.ToUInt16(packet, 2);

            // Validate connection
            if (!actor.Socket.Connected)
                return;

            try
            {
                // Switch on the packet type
                MsgBase<Client> msg = null;
                switch (type)
                {
                    case PacketType.MsgRegister:
                        msg = new MsgRegister();
                        break;

                    case PacketType.MsgTalk:
                        msg = new MsgTalk();
                        break;

                    case PacketType.MsgWalk:
                        msg = new MsgWalk();
                        break;

                    case PacketType.MsgItem:
                        msg = new MsgItem();
                        break;

                    case PacketType.MsgAction:
                        msg = new MsgAction();
                        break;

                    case PacketType.MsgName:
                        msg = new MsgName();
                        break;

                    case PacketType.MsgFriend:
                        msg = new MsgFriend();
                        break;

                    case PacketType.MsgInteract:
                        msg = new MsgInteract();
                        break;

                    case PacketType.MsgTeam:
                        msg = new MsgTeam();
                        break;

                    case PacketType.MsgAllot:
                        msg = new MsgAllot();
                        break;

                    case PacketType.MsgGemEmbed:
                        msg = new MsgGemEmbed();
                        break;

                    case PacketType.MsgGodExp:
                        msg = new MsgGodExp();
                        break;

                    case PacketType.MsgConnect:
                        msg = new MsgConnect();
                        break;

                    case PacketType.MsgTrade:
                        msg = new MsgTrade();
                        break;

                    case PacketType.MsgSynpOffer:
                        msg = new MsgSynpOffer();
                        break;

                    case PacketType.MsgMapItem:
                        msg = new MsgMapItem();
                        break;

                    case PacketType.MsgPackage:
                        msg = new MsgPackage();
                        break;

                    case PacketType.MsgSyndicate:
                        msg = new MsgSyndicate();
                        break;

                    case PacketType.MsgMessageBoard:
                        msg = new MsgMessageBoard();
                        break;

                    case PacketType.MsgInviteTrans:
                        msg = new MsgInviteTrans();
                        break;

                    case PacketType.MsgTitle:
                        msg = new MsgTitle();
                        break;

                    case PacketType.MsgTaskStatus:
                        msg = new MsgTaskStatus();
                        break;

                    case PacketType.MsgTaskDetailInfo:
                        msg = new MsgTaskDetailInfo();
                        break;

                    case PacketType.MsgRank:
                        msg = new MsgRank();
                        break;

                    case PacketType.MsgFlower:
                        msg = new MsgFlower();
                        break;

                    case PacketType.MsgFamily:
                        msg = new MsgFamily();
                        break;

                    case PacketType.MsgFamilyOccupy:
                        msg = new MsgFamilyOccupy();
                        break;

                    case PacketType.MsgNpc:
                        msg = new MsgNpc();
                        break;

                    case PacketType.MsgNpcInfo:
                        msg = new MsgNpcInfo();
                        break;

                    case PacketType.MsgTaskDialog:
                        msg = new MsgTaskDialog();
                        break;

                    case PacketType.MsgDataArray:
                        msg = new MsgDataArray();
                        break;

                    case PacketType.MsgTraining:
                        msg = new MsgTraining();
                        break;

                    case PacketType.MsgTradeBuddy:
                        msg = new MsgTradeBuddy();
                        break;

                    case PacketType.MsgEquipLock:
                        msg = new MsgEquipLock();
                        break;

                    case PacketType.MsgPigeon:
                        msg = new MsgPigeon();
                        break;

                    case PacketType.MsgPeerage:
                        msg = new MsgPeerage();
                        break;

                    case PacketType.MsgGuide:
                        msg = new MsgGuide();
                        break;

                    case PacketType.MsgGuideInfo:
                        msg = new MsgGuideInfo();
                        break;

                    case PacketType.MsgGuideContribute:
                        msg = new MsgGuideContribute();
                        break;

                    case PacketType.MsgQuiz:
                        msg = new MsgQuiz();
                        break;

                    case PacketType.MsgFactionRankInfo:
                        msg = new MsgFactionRankInfo();
                        break;

                    case PacketType.MsgSynMemberList:
                        msg = new MsgSynMemberList();
                        break;

                    case PacketType.MsgTotemPoleInfo:
                        msg = new MsgTotemPoleInfo();
                        break;

                    case PacketType.MsgWeaponsInfo:
                        msg = new MsgWeaponsInfo();
                        break;

                    case PacketType.MsgTotemPole:
                        msg = new MsgTotemPole();
                        break;

                    case PacketType.MsgQualifyingInteractive:
                        msg = new MsgQualifyingInteractive();
                        break;

                    case PacketType.MsgQualifyingFightersList:
                        msg = new MsgQualifyingFightersList();
                        break;

                    case PacketType.MsgQualifyingRank:
                        msg = new MsgQualifyingRank();
                        break;

                    case PacketType.MsgQualifyingSeasonRankList:
                        msg = new MsgQualifyingSeasonRankList();
                        break;

                    case PacketType.MsgQualifyingDetailInfo:
                        msg = new MsgQualifyingDetailInfo();
                        break;

                    case PacketType.MsgMentorPlayer:
                        msg = new MsgMentorPlayer();
                        break;

                    case PacketType.MsgSuitStatus:
                        msg = new MsgSuitStatus();
                        break;

                    default:
                        Console.WriteLine(
                            $@"Missing packet {type}, Length {length}
{PacketDump.Hex(packet)}");
                        await actor.SendAsync(new MsgTalk(actor.Identity, TalkChannel.Service,
                                                          string.Format("Missing packet {0}, Length {1}",
                                                                        type, length)));
                        return;
                }

                // Decode packet bytes into the structure and process
                msg.Decode(packet);
                // Packet has been decrypted and now will be queued in the region processor
                if (actor.Character?.Map != null)
                {
                    Character user = RoleManager.GetUser(actor.Character.Identity);
                    if (user == null || !user.Client.GUID.Equals(actor.GUID))
                    {
                        actor.Disconnect();
                        if (user != null)
                            await RoleManager.KickOutAsync(actor.Identity);
                        return;
                    }

                    Kernel.Services.Processor.Queue(actor.Character.Map.Partition, () => msg.ProcessAsync(actor));
                }
                else
                {
                    Kernel.Services.Processor.Queue(ServerProcessor.NO_MAP_GROUP, () => msg.ProcessAsync(actor));
                }
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
        protected override void Disconnected(Client actor)
        {
            if (actor == null)
            {
                Console.WriteLine(@"Disconnected with actor null ???");
                return;
            }

            Processor.DeselectPartition(actor.Partition);

            var fromCreation = false;
            if (actor.Creation != null)
            {
                Kernel.Registration.Remove(actor.Creation.Token);
                fromCreation = true;
            }

            if (actor.Character != null)
            {
                _ = Log.WriteLogAsync(LogLevel.Info, $"{actor.Character.Name} has logged out.").ConfigureAwait(false);

                Kernel.Services.Processor.Queue(ServerProcessor.NO_MAP_GROUP, async () => { await actor.Character.OnDisconnectAsync(); });
            }
            else
            {
                if (fromCreation)
                    _ = Log.WriteLogAsync(LogLevel.Info,
                                          $"{actor.AccountIdentity} has created a new character and has logged out.")
                           .ConfigureAwait(false);
                else
                    _ = Log.WriteLogAsync(LogLevel.Info, $"[{actor.IpAddress}] {actor.AccountIdentity} has logged out.")
                           .ConfigureAwait(false);
            }
        }
    }
}