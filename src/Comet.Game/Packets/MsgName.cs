using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgName : MsgName<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character targetUser;
            switch (Action)
            {
                case StringAction.QueryMate:
                {
                    targetUser = RoleManager.GetUser(Identity);
                    if (targetUser == null)
                        return;

                    Strings[0] = targetUser.MateName;
                    await client.Character.SendAsync(this);
                    break;
                }

                case StringAction.Guild:
                {
                    Syndicate syndicate = SyndicateManager.GetSyndicate((int) Identity);
                    if (syndicate == null)
                        return;

                    Strings.Add(syndicate.Name);
                    await client.Character.SendAsync(this);
                    break;
                }

                case StringAction.MemberList:
                {
                    if (client.Character.Syndicate == null)
                        return;

                    await client.Character.Syndicate.SendMembersAsync((int) Identity, client.Character);
                    break;
                }

                case StringAction.WhisperWindowInfo:
                {
                    if (Strings.Count == 0)
                    {
                        await client.SendAsync(this);
                        return;
                    }

                    targetUser = RoleManager.GetUser(Strings[0]);
                    if (targetUser == null)
                    {
                        await client.SendAsync(this);
                        return;
                    }

                    Strings.Add(
                        $"{targetUser.Identity} {targetUser.Level} {targetUser.BattlePower} #{targetUser.SyndicateName} #{targetUser.FamilyName} {targetUser.MateName} {(int) targetUser.NobilityRank} {targetUser.Gender}");
                    await client.SendAsync(this);
                    break;
                }
            }
        }
    }
}