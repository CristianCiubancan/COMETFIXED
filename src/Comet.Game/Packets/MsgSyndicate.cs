using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgSyndicate : MsgSyndicate<Client>
    {
        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Mode)
            {
                case SyndicateRequest.JoinRequest:
                {
                    if (Identity == 0 || user.Syndicate != null)
                        return;

                    Character leader = RoleManager.GetUser(Identity);
                    if (leader == null)
                        return;

                    if (leader.SyndicateIdentity == 0 ||
                        leader.SyndicateRank < SyndicateMember.SyndicateRank.Manager)
                        return;

                    if (user.QueryRequest(RequestType.Syndicate) == Identity)
                    {
                        user.PopRequest(RequestType.Syndicate);
                        await leader.Syndicate.AppendMemberAsync(user, leader, Syndicate.JoinMode.Invite);
                    }
                    else
                    {
                        leader.SetRequest(RequestType.Syndicate, user.Identity);
                        Identity = user.Identity;
                        await leader.SendAsync(this);
                        await leader.SendRelationAsync(user);
                    }

                    break;
                }

                case SyndicateRequest.InviteRequest:
                {
                    if (Identity == 0 || user.Syndicate == null)
                        return;

                    Character target = RoleManager.GetUser(Identity);
                    if (target == null)
                        return;

                    if (user.SyndicateIdentity == 0
                        || user.SyndicateRank < SyndicateMember.SyndicateRank.DeputyLeader)
                        return;

                    if (user.QueryRequest(RequestType.Syndicate) == Identity)
                    {
                        user.PopRequest(RequestType.Syndicate);
                        await user.Syndicate.AppendMemberAsync(target, user, Syndicate.JoinMode.Request);
                    }
                    else
                    {
                        target.SetRequest(RequestType.Syndicate, user.Identity);
                        Identity = user.Identity;
                        ConditionLevel = user.Syndicate.LevelRequirement;
                        ConditionMetempsychosis = user.Syndicate.MetempsychosisRequirement;
                        ConditionProfession = (int) user.Syndicate.ProfessionRequirement;
                        await target.SendAsync(this);
                        await target.SendRelationAsync(user);
                    }

                    break;
                }

                case SyndicateRequest.Quit: // 3
                {
                    if (user.SyndicateIdentity == 0)
                        return;

                    await user.Syndicate.QuitSyndicateAsync(user);
                    break;
                }

                case SyndicateRequest.Query: // 6
                {
                    Syndicate queryTarget = SyndicateManager.GetSyndicate((int) Identity);
                    if (queryTarget == null)
                        return;

                    await queryTarget.SendAsync(user);
                    break;
                }

                case SyndicateRequest.DonateSilvers:
                {
                    if (user.Syndicate == null)
                        return;

                    var amount = (int) Identity;
                    if (!await user.SpendMoneyAsync(amount))
                    {
                        await user.SendAsync(Language.StrNotEnoughMoney);
                        return;
                    }

                    user.Syndicate.Money += amount;
                    await user.Syndicate.SaveAsync();
                    user.SyndicateMember.Silvers += (int) Identity;
                    user.SyndicateMember.SilversTotal += Identity;
                    await user.SyndicateMember.SaveAsync();
                    await user.SendSyndicateAsync();

                    await user.Syndicate.SendAsync(string.Format(Language.StrSynDonateMoney, user.SyndicateRank,
                                                                 user.Name, Identity));
                    break;
                }

                case SyndicateRequest.Refresh: // 12
                {
                    if (user.Syndicate != null)
                    {
                        await user.SendSyndicateAsync();
                        await user.Syndicate.SendRelationAsync(user);
                        await user.Syndicate.SendMinContributionAsync(user);
                    }

                    break;
                }

                case SyndicateRequest.DonateConquerPoints:
                {
                    if (user.Syndicate == null)
                        return;

                    var amount = (int) Identity;
                    if (!await user.SpendConquerPointsAsync(amount))
                    {
                        await user.SendAsync(Language.StrNotEnoughEmoney);
                        return;
                    }

                    user.Syndicate.ConquerPoints += Identity;
                    await user.Syndicate.SaveAsync();
                    user.SyndicateMember.ConquerPointsDonation += Identity;
                    user.SyndicateMember.ConquerPointsTotalDonation += Identity;
                    await user.SyndicateMember.SaveAsync();
                    await user.SendSyndicateAsync();

                    await user.Syndicate.SendAsync(
                        string.Format(Language.StrSynDonateEmoney, user.SyndicateRank, user.Name, Identity));
                    break;
                }

                case SyndicateRequest.Bulletin:
                {
                    if (user?.Syndicate == null || user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return;

                    if (Strings.Count == 0)
                        return;

                    if (user.Syndicate.Money < Syndicate.SYNDICATE_ACTION_COST)
                    {
                        await user.SendAsync(string.Format(Language.StrSynNoMoney, Syndicate.SYNDICATE_ACTION_COST));
                        return;
                    }

                    string message = Strings[0].Substring(0, Math.Min(128, Strings[0].Length));

                    user.Syndicate.Announce = message;
                    user.Syndicate.AnnounceDate = DateTime.Now;
                    await user.Syndicate.SaveAsync();

                    await user.SendSyndicateAsync();
                    break;
                }

                case SyndicateRequest.Promotion:
                case SyndicateRequest.PaidPromotion:
                {
                    if (Strings.Count == 0)
                        return;

                    if (user?.Syndicate == null)
                        return;

                    await user.Syndicate.PromoteAsync(user, Strings[0], (SyndicateMember.SyndicateRank) Identity);
                    break;
                }

                case SyndicateRequest.PromotionList:
                {
                    if (user.Syndicate != null)
                        await user.Syndicate.SendPromotionListAsync(user);
                    break;
                }

                case SyndicateRequest.Discharge:
                case SyndicateRequest.DischargePaid:
                {
                    if (user.Syndicate == null || Strings.Count == 0)
                        return;

                    await user.Syndicate.DemoteAsync(user, Strings[0]);
                    break;
                }

                case SyndicateRequest.Ally:
                {
                    if (user.Syndicate == null)
                        return;

                    if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return;

                    if (user.Syndicate.AlliesCount >= user.Syndicate.MaxAllies())
                        return;

                    if (Strings.Count == 0)
                        return;

                    Syndicate target = SyndicateManager.GetSyndicate(Strings[0]);

                    if (target == null || target.Deleted)
                        return;

                    Character targetLeader = target.Leader.User;
                    if (targetLeader == null)
                        return;

                    if (targetLeader.MessageBox is CaptchaBox)
                    {
                        await user.SendAsync(Language.StrMessageBoxCannotCaptcha);
                        return;
                    }

                    var box = new SyndicateRelationBox(targetLeader);
                    if (!await box.CreateAsync(user, targetLeader, SyndicateRelationBox.RelationType.Ally))
                        return;

                    targetLeader.MessageBox = box;
                    break;
                }

                case SyndicateRequest.Unally:
                {
                    if (user.Syndicate == null)
                        return;
                    if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return;
                    Syndicate target = SyndicateManager.GetSyndicate(Strings[0]);
                    if (target == null)
                        return;

                    await user.Syndicate.DisbandAllianceAsync(user, target);
                    break;
                }

                case SyndicateRequest.Enemy:
                {
                    if (user.Syndicate == null)
                        return;

                    if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return;

                    if (user.Syndicate.EnemyCount >= user.Syndicate.MaxEnemies())
                        return;

                    if (Strings.Count == 0)
                        return;

                    Syndicate target = SyndicateManager.GetSyndicate(Strings[0]);
                    if (target == null || target.Deleted)
                        return;

                    await user.Syndicate.AntagonizeAsync(user, target);
                    break;
                }

                case SyndicateRequest.Unenemy:
                {
                    if (user.Syndicate == null)
                        return;
                    if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return;
                    Syndicate target = SyndicateManager.GetSyndicate(Strings[0]);
                    if (target == null)
                        return;

                    await user.Syndicate.PeaceAsync(user, target);
                    break;
                }

                case SyndicateRequest.SetRequirements:
                {
                    if (user.Syndicate == null)
                        return;
                    if (user.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return;

                    user.Syndicate.LevelRequirement = (byte) Math.Min(Role.MAX_UPLEV, Math.Max(1, ConditionLevel));
                    user.Syndicate.ProfessionRequirement =
                        (uint) Math.Min((uint) Syndicate.ProfessionPermission.All, Math.Max(0, ConditionProfession));
                    user.Syndicate.MetempsychosisRequirement = (byte) Math.Min(2, Math.Max(0, ConditionMetempsychosis));
                    await user.Syndicate.SaveAsync();
                    break;
                }

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"Type: {Type}, Subtype: {Mode} not handled");
                    break;
            }
        }
    }
}