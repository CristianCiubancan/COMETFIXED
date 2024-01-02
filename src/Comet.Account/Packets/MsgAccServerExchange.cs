using System;
using System.Linq;
using System.Threading.Tasks;
using Comet.Account.Database;
using Comet.Account.States;
using Comet.Database.Entities;
using Comet.Network.Packets.Internal;
using Comet.Network.Security;
using Comet.Shared;

namespace Comet.Account.Packets
{
    public sealed class MsgAccServerExchange : MsgAccServerExchange<GameServer>
    {
        public static byte[] RealmDataKey { get; set; }

        public override async Task ProcessAsync(GameServer client)
        {
            try
            {
                DbRealm realm =
                    Kernel.Realms.Values.FirstOrDefault(
                        x => x.Name.Equals(ServerName, StringComparison.InvariantCultureIgnoreCase));

                if (realm == null)
                {
                    await client.SendAsync(new MsgAccServerAction
                    {
                        Action = MsgAccServerAction<GameServer>.ServerAction.ConnectionResult,
                        Data = (int) MsgAccServerAction<GameServer>.ConnectionStatus.AuthorizationError
                    });
                    await Log.WriteLogAsync(LogLevel.Exception,
                                            $"Invalid server {ServerName} [Connection: {client.IpAddress}].");
                    client.Disconnect();
                    return;
                }

                string username = AesCipherHelper.Decrypt(RealmDataKey, realm.Username);
                string password = AesCipherHelper.Decrypt(RealmDataKey, realm.Password);

                if (!Username.Equals(username, StringComparison.InvariantCulture) ||
                    !Password.Equals(password, StringComparison.InvariantCulture))
                {
                    await client.SendAsync(new MsgAccServerAction
                    {
                        Action = MsgAccServerAction<GameServer>.ServerAction.ConnectionResult,
                        Data = (int) MsgAccServerAction<GameServer>.ConnectionStatus.InvalidUsernamePassword
                    });
                    await Log.WriteLogAsync(LogLevel.Exception,
                                            $"Invalid server {ServerName} Username or Password ({username}={Username}:{password}={Password})" +
                                            $" [Connection: {client.IpAddress}].");
                    client.Disconnect();
                    return;
                }

                if (!client.IpAddress.Equals(realm.RpcIPAddress))
                {
                    await client.SendAsync(new MsgAccServerAction
                    {
                        Action = MsgAccServerAction<GameServer>.ServerAction.ConnectionResult,
                        Data = (int) MsgAccServerAction<GameServer>.ConnectionStatus.AddressNotAuthorized
                    });
                    await Log.WriteLogAsync(LogLevel.Exception,
                                            $"Invalid server {ServerName} not authorized connection [Connection: {client.IpAddress}].");
                    client.Disconnect();
                    return;
                }

                await Log.WriteLogAsync(LogLevel.Info, $"Server [{realm.Name}] has authenticated gracefully.");
                realm.Server = client;
                client.SetRealm(realm.Name);

                realm.LastPing = DateTime.Now;
                realm.Status = DbRealm.RealmStatus.Online;
                await ServerDbContext.SaveAsync(realm);

                await client.SendAsync(new MsgAccServerAction
                {
                    Action = MsgAccServerAction<GameServer>.ServerAction.ConnectionResult,
                    Data = (int) MsgAccServerAction<GameServer>.ConnectionStatus.Success
                });
            }
            catch (Exception ex)
            {
                await client.SendAsync(new MsgAccServerAction
                {
                    Action = MsgAccServerAction<GameServer>.ServerAction.ConnectionResult,
                    Data = (int) MsgAccServerAction<GameServer>.ConnectionStatus.AuthorizationError
                });
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                client.Disconnect();
            }
        }
    }
}