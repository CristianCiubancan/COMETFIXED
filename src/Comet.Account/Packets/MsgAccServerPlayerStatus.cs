using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Comet.Account.Database;
using Comet.Account.Database.Repositories;
using Comet.Account.States;
using Comet.Database.Entities;
using Comet.Network.Packets.Internal;
using Comet.Shared;

namespace Comet.Account.Packets
{
    public sealed class MsgAccServerPlayerStatus : MsgAccServerPlayerStatus<GameServer>
    {
        public override async Task ProcessAsync(GameServer client)
        {
            DbRealm realm = Kernel.Realms.Values.FirstOrDefault(x => x.Name.Equals(ServerName));
            if (realm == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Invalid server name [{ServerName}] tried to update data from [{client.IpAddress}].");
                return;
            }

            if (realm.Server == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"{ServerName} is not connected and tried to update player status from [{client.IpAddress}].");
                return;
            }

            if (Count == 0)
                return;

            foreach (PlayerStatus info in Status)
            {
                if (info.Online)
                {
                    DbAccount account = await AccountsRepository.FindAsync(info.AccountIdentity);
                    if (account == null)
                    {
                        await client.SendAsync(new MsgAccServerCmd
                        {
                            Action = MsgAccServerCmd<GameServer>.ServerAction.Disconnect,
                            AccountIdentity = info.AccountIdentity
                        });
                        await Log.WriteLogAsync(LogLevel.Info,
                                                $"User [{info.Identity}] being disconnected due to invalid account.");
                        return;
                    }

                    if ((account.StatusID & DbAccount.AccountStatus.Banned) != 0
                        || (account.StatusID & DbAccount.AccountStatus.Locked) != 0
                        || (account.StatusID & DbAccount.AccountStatus.NotActivated) != 0)
                    {
                        await client.SendAsync(new MsgAccServerCmd
                        {
                            Action = MsgAccServerCmd<GameServer>.ServerAction.Disconnect,
                            AccountIdentity = info.AccountIdentity
                        });
                        await Log.WriteLogAsync(LogLevel.Info,
                                                $"User [{info.Identity}] being disconnected due to banned account. Flag [{account.StatusID}]");
                        return;
                    }

                    Kernel.Players.TryAdd(info.Identity, new Player
                    {
                        Account = account,
                        AccountIdentity = account.AccountID,
                        Realm = realm
                    });
                }
                else
                {
                    Kernel.Players.TryRemove(info.Identity, out _);

                    if (info.Deleted)
                    {
                        var player = await RecordUserRepository.GetByIdAsync(info.Identity, client.Realm.RealmID);
                        if (player != null)
                        {
                            player.DeletedAt = DateTime.Now;
                            await ServerDbContext.SaveAsync(player);
                        }
                    }
                }
            }
        }
    }
}