using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Families;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgFamily : MsgFamily<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = RoleManager.GetUser(client.Character?.Identity ?? 0);
            if (user == null)
            {
                client.Disconnect();
                return;
            }

            switch (Action)
            {
                case FamilyAction.Query:
                {
                    if (user.Family == null)
                        return;

                    await user.SendFamilyAsync();
                    break;
                }

                case FamilyAction.QueryMemberList:
                {
                    if (user.Family == null)
                        return;

                    await user.Family.SendMembersAsync(0, user);
                    break;
                }

                case FamilyAction.Recruit:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (user.Family.PureMembersCount >= Family.MAX_MEMBERS)
                        return;

                    Character target = RoleManager.GetUser(Identity);
                    if (target is not {Family: null})
                        return;

#if !DEBUG
                    if (target.IsPm() && !user.IsPm())
                        return;
#endif

                    user.SetRequest(RequestType.Family, target.Identity);

                    Strings.Clear();

                    Identity = user.FamilyIdentity;
                    Strings.Add(user.FamilyName);
                    Strings.Add(user.Name);
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);
                    break;
                }

                case FamilyAction.AcceptRecruit:
                {
                    if (user.Family != null)
                        return;

                    Family family = FamilyManager.GetFamily(Identity);
                    if (family == null)
                        return;

                    if (family.PureMembersCount >= Family.MAX_MEMBERS)
                        return;

                    Character leader = family.Leader.User;
                    if (leader == null)
                        return;

                    if (leader.QueryRequest(RequestType.Family) != user.Identity)
                        return;

                    leader.PopRequest(RequestType.Family);
                    await family.AppendMemberAsync(leader, user);
                    break;
                }

                case FamilyAction.Join:
                {
                    if (user.Family != null)
                        return;

                    Character leader = RoleManager.GetUser(Identity);
                    if (leader is not {FamilyPosition: Family.FamilyRank.ClanLeader})
                        return;
                    if (leader.Family.PureMembersCount >= Family.MAX_MEMBERS)
                        return;

#if !DEBUG
                    if (leader.IsPm() && !user.IsPm())
                        return;
#endif

                    user.SetRequest(RequestType.Family, leader.Identity);

                    Strings.Clear();

                    Identity = user.Identity;
                    Strings.Add(user.Name);
                    await leader.SendAsync(this);
                    await leader.SendRelationAsync(user);
                    break;
                }

                case FamilyAction.AcceptJoinRequest:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    Character requester = RoleManager.GetUser(Identity);
                    if (requester == null)
                        return;

                    if (requester.Family != null)
                        return;

                    if (requester.QueryRequest(RequestType.Family) != user.Identity)
                        return;

                    requester.PopRequest(RequestType.Family);
                    await user.Family.AppendMemberAsync(user, requester);
                    break;
                }

                case FamilyAction.AddEnemy:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (user.Family.EnemyCount >= Family.MAX_RELATION)
                        return;
                    if (Strings.Count == 0)
                        return;
                    Family target = FamilyManager.GetFamily(Strings[0]);
                    if (target == null)
                        return;
                    if (user.Family.IsEnemy(target.Identity) || user.Family.IsAlly(target.Identity))
                        return;
                    user.Family.SetEnemy(target);
                    await user.Family.SaveAsync();
                    await user.Family.SendRelationsAsync();
                    break;
                }

                case FamilyAction.DeleteEnemy:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (Strings.Count == 0)
                        return;
                    Family target = FamilyManager.GetFamily(Strings[0]);
                    if (target == null)
                        return;
                    if (!user.Family.IsEnemy(target.Identity))
                        return;
                    user.Family.UnsetEnemy(target.Identity);
                    await user.Family.SaveAsync();

                    Identity = target.Identity;
                    await user.Family.SendAsync(this);
                    break;
                }

                case FamilyAction.AddAlly:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (user.Family.AllyCount >= Family.MAX_RELATION)
                        return;
                    if (Identity == 0)
                        return;
                    Character targetUser = RoleManager.GetUser(Identity);
                    if (targetUser == null)
                        return;

                    if (targetUser.FamilyIdentity == 0 || targetUser.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    Family target = targetUser.Family;
                    if (target == null || !target.Leader.IsOnline)
                        return;
                    if (user.Family.IsEnemy(target.Identity) || user.Family.IsAlly(target.Identity))
                        return;

                    Strings.Clear();
                    Identity = user.Family.Identity;
                    Strings = new List<string>
                    {
                        user.FamilyName,
                        user.Name
                    };

                    await target.Leader.User.SendAsync(this);
                    break;
                }

                case FamilyAction.AcceptAlliance:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (user.Family.AllyCount >= Family.MAX_RELATION)
                        return;
                    if (Strings.Count == 0)
                        return;

                    Character targetUser = RoleManager.GetUser(Strings[0]);
                    if (targetUser == null)
                        return;

                    if (targetUser.FamilyIdentity == 0 || targetUser.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    Family target = targetUser.Family;
                    if (target == null)
                        return;
                    if (user.Family.IsEnemy(target.Identity) || user.Family.IsAlly(target.Identity))
                        return;

                    user.Family.SetAlly(target);
                    await user.Family.SaveAsync();

                    target.SetAlly(user.Family);
                    await target.SaveAsync();

                    await user.Family.SendRelationsAsync();
                    await target.SendRelationsAsync();
                    break;
                }

                case FamilyAction.DeleteAlly:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (Strings.Count == 0)
                        return;
                    Family target = FamilyManager.GetFamily(Strings[0]);
                    if (target == null)
                        return;
                    if (!user.Family.IsAlly(target.Identity))
                        return;
                    user.Family.UnsetAlly(target.Identity);
                    await user.Family.SaveAsync();

                    target.UnsetAlly(user.FamilyIdentity);
                    await target.SaveAsync();

                    Identity = target.Identity;
                    await user.Family.SendAsync(this);

                    Identity = user.FamilyIdentity;
                    Strings = new List<string> {user.FamilyName};
                    await target.SendAsync(this);
                    break;
                }

                case FamilyAction.Abdicate:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (Strings.Count == 0)
                        return;
                    Character target = RoleManager.GetUser(Strings[0]);
                    if (target == null)
                        return;
                    if (target.FamilyIdentity != user.FamilyIdentity)
                        return;
                    if (target.FamilyPosition != Family.FamilyRank.Member)
                        return;
                    await user.Family.AbdicateAsync(user, Strings[0]);
                    break;
                }

                case FamilyAction.KickOut:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;
                    if (Strings.Count == 0)
                        return;
                    FamilyMember target = user.Family.GetMember(Strings[0]);
                    if (target == null)
                        return;
                    if (target.FamilyIdentity != user.FamilyIdentity)
                        return;
                    if (target.Rank != Family.FamilyRank.Member)
                        return;
                    await user.Family.KickOutAsync(user, target.Identity);
                    break;
                }

                case FamilyAction.Quit:
                {
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition == Family.FamilyRank.ClanLeader)
                        return;
                    if (user.FamilyPosition == Family.FamilyRank.Spouse)
                        return;

                    await user.Family.LeaveAsync(user);
                    break;
                }

                case FamilyAction.Announce:
                {
                    if (user.Family == null)
                        return;

                    Identity = user.FamilyIdentity;
                    Strings.Add(user.Family.Announcement);
                    await user.SendAsync(this);
                    break;
                }

                case FamilyAction.SetAnnouncement:
                {
                    if (Strings.Count == 0)
                        return;
                    if (user.Family == null)
                        return;
                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    user.Family.Announcement = Strings[0].Substring(0, Math.Min(127, Strings[0].Length));
                    await user.Family.SaveAsync();

                    Action = FamilyAction.Announce;
                    await user.Family.SendAsync(this);
                    break;
                }

                case FamilyAction.Dedicate:
                {
                    if (user.Family == null)
                        return;

                    if (!await user.SpendMoneyAsync((int) Identity, true))
                        return;

                    user.Family.Money += Identity;
                    user.FamilyMember.Proffer += Identity;
                    await user.Family.SaveAsync();
                    await user.FamilyMember.SaveAsync();
                    await user.SendFamilyAsync();
                    break;
                }

                case FamilyAction.QueryOccupy:
                {
                    if (user.Family == null)
                        return;

                    await user.SendFamilyAsync();
                    await user.SendFamilyOccupyAsync();
                    break;
                }

                default:
                {
                    if (user.IsPm())
                        await client.SendAsync(new MsgTalk(client.Identity, TalkChannel.Service,
                                                           $"Missing packet {Type}, Action {Action}, Length {Length}"));

                    await Log.WriteLogAsync(LogLevel.Warning,
                                            "Missing packet {0}, Action {1}, Length {2}\n{3}",
                                            Type, Action, Length, PacketDump.Hex(Encode()));
                    break;
                }
            }
        }
    }
}