using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgTeam : MsgTeam<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = RoleManager.GetUser(Identity);
            if (target == null && Identity != 0)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Team no target");
                return;
            }

#if !DEBUG
            if (user.IsGm() && target != null && !target.IsPm())
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"GM Character trying to team with no GM");
                return;
            }
#endif

            if (user.Map.IsTeamDisable())
                return;

            if (user.Team != null && !user.Team.IsLeader(user.Identity))
                await user.DetachStatusAsync(StatusSet.TEAM_LEADER);

            switch (Action)
            {
                case TeamAction.Create:
                    if (user.Team != null)
                        return;

                    var team = new Team(user);
                    if (!team.Create())
                        return;

                    await user.SendAsync(this);
                    await user.AttachStatusAsync(user, StatusSet.TEAM_LEADER, 0, int.MaxValue, 0, 0);
                    break;

                case TeamAction.Dismiss:
                    if (user.Team == null)
                        return;

                    if (await user.Team.DismissAsync(user))
                    {
                        await user.SendAsync(this);
                        await user.DetachStatusAsync(StatusSet.TEAM_LEADER);
                    }

                    break;

                case TeamAction.RequestJoin:
                    if (target == null)
                        return;

                    if (user.Team != null)
                    {
                        await user.SendAsync(Language.StrTeamAlreadyNoJoin);
                        return;
                    }

                    if (target.Identity == user.Identity || user.GetDistance(target) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTeamLeaderNotInRange);
                        return;
                    }

                    if (target.Team == null)
                    {
                        await user.SendAsync(Language.StrNoTeam);
                        return;
                    }

                    if (!target.Team.JoinEnable)
                    {
                        await user.SendAsync(Language.StrTeamClosed);
                        return;
                    }

                    if (target.Team.MemberCount >= Team.MAX_MEMBERS)
                    {
                        await user.SendAsync(Language.StrTeamFull);
                        return;
                    }

                    if (!target.IsAlive)
                    {
                        await user.SendAsync(Language.StrTeamLeaderDead);
                        return;
                    }

                    if (!target.Team.IsLeader(target.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoLeader);
                        return;
                    }

                    user.SetRequest(RequestType.TeamApply, target.Identity);
                    Identity = user.Identity;
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);

                    await user.SendAsync(Language.StrTeamApplySent);
                    break;

                case TeamAction.AcceptJoin:
                    if (target == null)
                        return;

                    if (user.Team == null)
                    {
                        await user.SendAsync(Language.StrNoTeamToInvite);
                        return;
                    }

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    if (user.Team.MemberCount >= Team.MAX_MEMBERS)
                    {
                        await user.SendAsync(Language.StrTeamFull);
                        return;
                    }

                    if (user.GetDistance(target) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTeamTargetNotInRange);
                        return;
                    }

                    if (target.Team != null)
                    {
                        await user.SendAsync(Language.StrTeamTargetAlreadyTeam);
                        return;
                    }

                    uint application = target.QueryRequest(RequestType.TeamApply);
                    if (application == user.Identity)
                    {
                        target.PopRequest(RequestType.TeamApply);
                        await user.SendAsync(this);
                        await user.Team.EnterTeamAsync(target);
                    }
                    else
                    {
                        await user.SendAsync(Language.StrTeamTargetHasNotApplied);
                    }

                    break;

                case TeamAction.RequestInvite:
                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTeamInvitedNotFound);
                        return;
                    }

                    if (user.Team == null)
                    {
                        await user.SendAsync(Language.StrNoTeam);
                        return;
                    }

                    if (!user.Team.JoinEnable)
                    {
                        await user.SendAsync(Language.StrTeamClosed);
                        return;
                    }

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    if (user.Team.MemberCount >= Team.MAX_MEMBERS)
                    {
                        await user.SendAsync(Language.StrTeamFull);
                        return;
                    }

                    if (target.Team != null)
                    {
                        await user.SendAsync(Language.StrTeamTargetAlreadyTeam);
                        return;
                    }

                    if (!target.IsAlive)
                    {
                        await user.SendAsync(Language.StrTargetIsNotAlive);
                        return;
                    }

                    user.SetRequest(RequestType.TeamInvite, target.Identity);

                    Identity = user.Identity;
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);

                    await user.SendAsync(Language.StrInviteSent);
                    break;

                case TeamAction.AcceptInvite:
                    if (user.Team != null)
                        // ?? send message
                        return;

                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTeamTargetNotInRange);
                        return;
                    }

                    if (target.Team == null)
                    {
                        await user.SendAsync(Language.StrTargetHasNoTeam);
                        return;
                    }

                    if (target.Team.MemberCount >= Team.MAX_MEMBERS)
                    {
                        await user.SendAsync(Language.StrTeamFull);
                        return;
                    }

                    if (!target.Team.IsLeader(target.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoLeader);
                        return;
                    }

                    uint inviteApplication = target.QueryRequest(RequestType.TeamInvite);
                    if (inviteApplication == user.Identity)
                    {
                        target.PopRequest(RequestType.TeamInvite);
                        await target.SendAsync(this);
                        await target.Team.EnterTeamAsync(user);
                    }
                    else
                    {
                        await user.SendAsync(Language.StrTeamNotInvited);
                    }

                    break;

                case TeamAction.LeaveTeam:
                    if (user.Team == null)
                        return;

                    if (user.Team.IsLeader(user.Identity))
                    {
                        await user.Team.DismissAsync(user);
                        return;
                    }

                    await user.Team.DismissMemberAsync(user);
                    await user.SendAsync(this);
                    break;

                case TeamAction.Kick:
                    if (user.Team == null || user.Team.IsLeader(user.Identity))
                        return;
                    if (target?.Team == null || target.Team.IsLeader(target.Identity))
                        return;

                    await user.Team.KickMemberAsync(user, Identity);
                    break;

                case TeamAction.Forbid:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.JoinEnable = false;
                    break;

                case TeamAction.RemoveForbid:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.JoinEnable = true;
                    break;

                case TeamAction.CloseMoney:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.MoneyEnable = false;
                    break;

                case TeamAction.OpenMoney:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.MoneyEnable = true;
                    break;

                case TeamAction.CloseItem:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.ItemEnable = false;
                    break;

                case TeamAction.OpenItem:
                    if (user.Team == null)
                        return;

                    if (!user.Team.IsLeader(user.Identity))
                    {
                        await user.SendAsync(Language.StrTeamNoCapitain);
                        return;
                    }

                    user.Team.ItemEnable = true;
                    break;
            }
        }
    }
}