using System.IO;
using System.Text;
using System.Threading.Tasks;
using Comet.Account.Database.Repositories;
using Comet.Account.States;
using Comet.Database.Entities;
using Comet.Network.Packets;
using Comet.Network.Security;
using Comet.Shared;

namespace Comet.Account.Packets
{
    using static MsgConnectEx;

    /// <remarks>Packet Type 1086</remarks>
    /// <summary>
    ///     Message containing login credentials from the login screen. This is the first
    ///     packet sent to the account server from the client on login. The server checks the
    ///     encrypted password against the hashed password in the database, the responds with
    ///     <see cref="MsgConnectEx" /> with either a pass or fail.
    /// </summary>
    public sealed class MsgAccount : MsgBase<Client>
    {
        // Packet Properties
        public string Username { get; private set; }
        public byte[] Password { get; private set; }
        public string Realm { get; private set; }

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            // Fetch account info from the database
            client.Account = await AccountsRepository.FindAsync(Username).ConfigureAwait(false);
            if (client.Account == null || !ConquerAccount.CheckPassword(
                    DecryptPassword(Password, client.Seed), client.Account.Password, client.Account.Salt))
            {
                await Log.WriteLogAsync("login_fail", LogLevel.Info,
                                        $"[{Username}] tried to login with an invalid account or password.");
                await client.SendAsync(new MsgConnectEx(RejectionCode.InvalidPassword));
                await client.Socket.DisconnectAsync(false);
                return;
            }

            if (client.Account.StatusID.HasFlag(DbAccount.AccountStatus.Banned)) // Banned
            {
                await Log.WriteLogAsync("login_fail", LogLevel.Info,
                                        $"[{Username}] has tried to login with a banned account.");
                await client.SendAsync(new MsgConnectEx(RejectionCode.AccountBanned));
                await client.Socket.DisconnectAsync(false);
                return;
            }

            if (client.Account.StatusID.HasFlag(DbAccount.AccountStatus.Locked)) // suspicious? temp lock
            {
                await Log.WriteLogAsync("login_fail", LogLevel.Info,
                                        $"[{Username}] has tried to login with a locked account.");
                await client.SendAsync(new MsgConnectEx(RejectionCode.AccountLocked));
                await client.Socket.DisconnectAsync(false);
                return;
            }

            if (client.Account.StatusID.HasFlag(DbAccount.AccountStatus.NotActivated))
            {
                await Log.WriteLogAsync("login_fail", LogLevel.Info,
                                        $"[{Username}] has tried to login with a not activated account.");
                await client.SendAsync(new MsgConnectEx(RejectionCode.AccountNotActivated));
                await client.Socket.DisconnectAsync(false);
                return;
            }

            // Connect to the game server
            if (!Kernel.Realms.TryGetValue(Realm, out DbRealm server) ||
                server.GetServer<GameServer>()?.Socket.Connected != true)
            {
                await Log.WriteLogAsync("login_fail", LogLevel.Info,
                                        $"[{Username}] tried to login on a not connected [{Realm}] server.");
                await client.SendAsync(new MsgConnectEx(RejectionCode.ServerMaintenance));
                await client.Socket.DisconnectAsync(false);
                return;
            }

            client.Realm = server;

            Kernel.Clients.TryAdd(client.Account.AccountID, client);

#if DEBUG
            await Log.WriteLogAsync(LogLevel.Info, $"Client [{client.GUID}] is awaiting for auth.");
#endif

            await server.GetServer<GameServer>().SendAsync(new MsgAccServerLoginExchange
            {
                AccountID = client.Account.AccountID,
                AuthorityID = client.Account.AuthorityID,
                IPAddress = client.IpAddress,
                AuthorityName = "",
                LastLogin = 0,
                VipLevel = 0
            });
        }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Username = reader.ReadString(16);
            reader.BaseStream.Seek(132, SeekOrigin.Begin);
            Password = reader.ReadBytes(16);
            reader.BaseStream.Seek(260, SeekOrigin.Begin);
            Realm = reader.ReadString(16);
        }

        /// <summary>
        ///     Decrypts the password from read in packet bytes for the <see cref="Decode" />
        ///     method. Trims the end of the password string of null terminators.
        /// </summary>
        /// <param name="buffer">Bytes from the packet buffer</param>
        /// <param name="seed">Seed for generating RC5 keys</param>
        /// <returns>Returns the decrypted password string.</returns>
        private string DecryptPassword(byte[] buffer, uint seed)
        {
            var rc5 = new RC5(seed);
            var scanCodes = new ScanCodeCipher(Username);
            var password = new byte[16];
            rc5.Decrypt(buffer, password);
            scanCodes.Decrypt(password, password);
            return Encoding.ASCII.GetString(password).Trim('\0');
        }
    }
}