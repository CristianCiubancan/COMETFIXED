using System;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgTotemPole : MsgTotemPole<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = RoleManager.GetUser(client.Character.Identity);
            if (user == null)
            {
                client.Disconnect();
                return;
            }

            if (user.SyndicateIdentity == 0)
                return;

            switch (Action)
            {
                case ActionMode.UnlockArsenal:
                {
                    if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader
                        && user.SyndicateRank != SyndicateMember.SyndicateRank.DeputyLeader
                        && user.SyndicateRank != SyndicateMember.SyndicateRank.HonoraryDeputyLeader
                        && user.SyndicateRank != SyndicateMember.SyndicateRank.LeaderSpouse)
                        return;

                    var type = (Syndicate.TotemPoleType) Data1;
                    if (type == Syndicate.TotemPoleType.None)
                        return;

                    if (user.Syndicate.LastOpenTotem != null)
                    {
                        int now = int.Parse($"{DateTime.Now:yyyyMMdd}");
                        int lastOpenTotem = int.Parse($"{user.Syndicate.LastOpenTotem.Value:yyyyMMdd}");
                        if (lastOpenTotem >= now)
                            return;
                    }

                    int price = user.Syndicate.UnlockTotemPolePrice();
                    if (user.Syndicate.Money < price)
                        return;

                    if (!await user.Syndicate.OpenTotemPoleAsync(type))
                        return;

                    user.Syndicate.Money -= price;
                    await user.Syndicate.SaveAsync();

                    await user.Syndicate.SendTotemPolesAsync(user);
                    await user.SendSyndicateAsync();
                    break;
                }

                case ActionMode.InscribeItem:
                {
                    Item item = user.UserPackage[(uint) Data2];
                    if (item == null)
                        return;

                    await user.Syndicate.InscribeItemAsync(user, item);
                    break;
                }

                case ActionMode.UnsubscribeItem:
                {
                    await user.Syndicate.UnsubscribeItemAsync((uint) Data2, user.Identity);
                    break;
                }

                case ActionMode.Enhance:
                {
                    if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return;

                    if (await user.Syndicate.EnhanceTotemPoleAsync((Syndicate.TotemPoleType) Data1, (byte) Data3))
                    {
                        await user.Syndicate.SendTotemPolesAsync(user);
                        await user.SendSyndicateAsync();
                    }

                    break;
                }

                case ActionMode.Refresh:
                {
                    await user.Syndicate.SendTotemPolesAsync(user);
                    break;
                }
            }
        }
    }
}