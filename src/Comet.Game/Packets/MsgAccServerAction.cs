using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.Internal.Auth;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Internal;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgAccServerAction : MsgAccServerAction<AccountServer>
    {
        public override async Task ProcessAsync(AccountServer client)
        {
            switch (Action)
            {
                case ServerAction.ConnectionResult:
                {
                    var status = (ConnectionStatus) Data;
                    if (status.Equals(ConnectionStatus.Success))
                    {
                        await Log.WriteLogAsync(LogLevel.Info, "Authenticated successfully with the realm server.");
                    }
                    else if (status.Equals(ConnectionStatus.AddressNotAuthorized))
                    {
                        await Log.WriteLogAsync(LogLevel.Socket,
                                                "This IP Address is not authorized to authenticate in the realms server.");
                        return;
                    }
                    else if (status.Equals(ConnectionStatus.AuthorizationError))
                    {
                        await Log.WriteLogAsync(LogLevel.Socket, "Invalid realm authorization information.");
                        return;
                    }
                    else if (status.Equals(ConnectionStatus.InvalidUsernamePassword))
                    {
                        await Log.WriteLogAsync(LogLevel.Socket, "Invalid realm Username or password.");
                        return;
                    }

                    if (RoleManager.OnlinePlayers > 0)
                    {
                        // let's send all players data...
                        List<Character> players = RoleManager.QueryUserSet();
                        var statuses = new MsgAccServerPlayerStatus();
                        var msg = new MsgAccServerPlayerExchange();
                        msg.ServerName = msg.ServerName = Kernel.GameConfiguration.ServerName;
                        var idx = 0;
                        foreach (Character player in players)
                        {
                            statuses.Status.Add(new MsgAccServerPlayerStatus<AccountServer>.PlayerStatus
                            {
                                Identity = player.Client.AccountIdentity,
                                Online = true
                            });

                            msg.Data.Add(MsgAccServerPlayerExchange.CreatePlayerData(player));

                            if (idx > 0 && idx % 30 == 0)
                            {
                                await client.SendAsync(msg);
                                msg.Data.Clear();
                            }

                            if (idx > 0 && idx % 500 == 0)
                            {
                                await client.SendAsync(statuses);
                                statuses.Status.Clear();
                            }

                            idx++;
                        }

                        if (msg.Data.Count > 0)
                            await client.SendAsync(msg);
                        if (statuses.Status.Count > 0)
                            await client.SendAsync(statuses);
                    }

                    break;
                }
            }
        }
    }
}