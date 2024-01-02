using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;
using Comet.Shared.Models;
using static Comet.Game.Packets.MsgTalk;

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1052</remarks>
    /// <summary>
    ///     Message containing a connection request to the game server. Contains the player's
    ///     access token from the Account server, and the patch and language versions of the
    ///     game client.
    /// </summary>
    public sealed class MsgConnect : MsgConnect<Client>
    {
        // Static properties from server initialization
        public static bool StrictAuthentication { get; set; }

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            // Validate access token
            var auth = Kernel.Logins.Get(Token.ToString()) as TransferAuthArgs;
            if (auth == null || StrictAuthentication && auth.IPAddress == client.IpAddress)
            {
                if (auth != null)
                    Kernel.Logins.Remove(Token.ToString());

                await client.SendAsync(LoginInvalid);
                await Log.WriteLogAsync(LogLevel.Warning, $"Invalid Login Token: {Token} from {client.IpAddress}");
                await client.Socket.DisconnectAsync(false);
                return;
            }

            Kernel.Logins.Remove(Token.ToString());

            // Generate new keys and check for an existing character
            DbCharacter character = await CharactersRepository.FindAsync(auth.AccountID);
            client.AccountIdentity = auth.AccountID;
            client.AuthorityLevel = auth.AuthorityID;
            client.MacAddress = MacAddress;

            // temp code for pre-release
#if DEBUG
            if (client.AuthorityLevel < 2)
            {
                await client.SendAsync(new MsgConnectEx(MsgConnectEx<Client>.RejectionCode.NonCooperatorAccount));
                await Log.WriteLogAsync(LogLevel.Warning, $"{client.Identity} non cooperator account.");
                await client.Socket.DisconnectAsync(false);
                return;
            }
#endif

            if (character == null)
            {
                // Create a new character
                client.Creation = new Creation {AccountID = auth.AccountID, Token = (uint) Token};
                Kernel.Registration.Add(client.Creation.Token);
                await client.SendAsync(LoginNewRole);
            }
            else
            {
                // The character exists, so we will turn the timeout back.
                client.ReceiveTimeOutSeconds = 30; // 30 seconds or DC

                // Character already exists
                client.Character = new Character(character, client);
                if (await RoleManager.LoginUserAsync(client))
                {
                    client.Character.MateName =
                        (await CharactersRepository.FindByIdentityAsync(client.Character.MateIdentity))?.Name ??
                        Core.Language.StrNone;
                    await client.SendAsync(LoginOk);
                    await client.SendAsync(new MsgUserInfo(client.Character));
                    await client.SendAsync(new MsgData());
#if DEBUG
                    await client.Character.SendAsync($"Server is running in DEBUG mode. Version: {Kernel.Version}",
                                                     TalkChannel.Talk);
#endif
                }
            }
        }
    }
}