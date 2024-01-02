using System;
using System.Drawing;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Game.States.Families;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgFamilyOccupy : MsgFamilyOccupy<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            var war = EventManager.GetEvent<FamilyWar>();
            if (war == null)
                return;

            switch (Action)
            {
                case FamilyPromptType.Challenge:
                {
                    if (user.Family == null)
                        return;

                    if (user.Family.ChallengeMap == Identity)
                        return;

                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    if ((DateTime.Now - user.Family.CreationDate).TotalDays < 1)
                    {
                        // not enough time
                        return;
                    }

                    var npc = RoleManager.FindRole<DynamicNpc>(x => x.Identity == Identity);
                    if (npc == null)
                        return;

                    uint fee = war.GetGoldFee(Identity);
                    if (fee == 0)
                        return;

                    if (user.Family.Money < fee)
                    {
                        await user.SendAsync(Language.StrNotEnoughFamilyMoneyToChallenge);
                        return;
                    }

                    user.Family.Money -= fee;
                    user.Family.ChallengeMap = (uint) npc.Data1;
                    user.Family.ChallengeTime = UnixTimestamp.Now();
                    await user.Family.SaveAsync();
                    await user.SendFamilyAsync();

                    GameMap map = MapManager.GetMap(user.Family.ChallengeMap);
                    if (map == null) //??
                        return;

                    await user.Family.SendAsync(string.Format(Language.StrPrepareToChallengeFamily, map.Name));

                    Family owner = war.GetFamilyOwner(npc.Identity);
                    if (owner != null)
                        await owner.SendAsync(string.Format(Language.StrPrepareToDefendFamily, map.Name));

                    break;
                }

                case FamilyPromptType.CancelChallenge:
                {
                    if (user.Family == null)
                        return;

                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    user.Family.ChallengeMap = 0;
                    user.Family.ChallengeTime = 0;
                    await user.Family.SaveAsync();
                    await user.SendFamilyAsync();
                    break;
                }

                case FamilyPromptType.RequestNpc:
                {
                    DailyPrize = war.GetNextReward(user, RequestNpc);
                    WeeklyPrize = war.GetNextWeekReward(user, RequestNpc);

                    var npc = user.Map.QueryRole<DynamicNpc>(RequestNpc);
                    Family owner = war.GetFamilyOwner(RequestNpc);
                    Identity = RequestNpc;
                    if (owner != null)
                    {
                        OccupyDays = owner.OccupyDays;
                        OccupyName = owner.Name;
                    }

                    if (owner?.Identity == user.FamilyIdentity)
                    {
                        WarRunning = war.IsAllowedToJoin(user);
                        SubAction = user.Identity == owner.LeaderIdentity ? 1u : 2u;

                        CanClaimRevenue = owner.LeaderIdentity == user.Identity && war.HasRewardToClaim(user);
                        CanClaimExperience = war.HasExpToClaim(user);

                        IsChallenged = war.GetChallengersByMap((uint) npc.Data1).Count > 0 ? 1u : 0u;
                    }
                    else
                    {
                        WarRunning = war.IsAllowedToJoin(user) && user.Family != null && user.Family.ChallengeMap == npc?.Data1;
                        CanRemoveChallenge = npc?.Data1 == user.Family?.ChallengeMap && !WarRunning;
                        if (CanRemoveChallenge)
                        {
                            SubAction = 3;
                        }
                        else
                        {
                            CanApplyChallenge = user.Family != null && RequestNpc != user.Family.ChallengeMap &&
                                                !WarRunning;
                            if (CanApplyChallenge)
                                SubAction = 5;
                        }
                    }

                    GoldFee = war.GetGoldFee(RequestNpc);
                    await user.SendAsync(this);
                    break;
                }

                case FamilyPromptType.AnnounceWarAccept:
                {
                    if (user.Family == null)
                        return;

                    if (war.IsInTime)
                        return;

                    if (!war.IsAllowedToJoin(user))
                        return;

                    DynamicNpc npc = war.GetDominatingNpc(user.Family);
                    if (npc == null)
                    {
                        npc = war.GetChallengeNpc(user.Family);
                        if (npc == null)
                            return;
                    }

                    GameMap map = MapManager.GetMap((uint) npc.Data1);
                    if (map == null)
                        return;

                    if ((DateTime.Now - user.FamilyMember.JoinDate).TotalHours < 24)
                        return;

                    Point targetPos = await map.QueryRandomPositionAsync();
                    if (targetPos.Equals(default))
                        targetPos = new Point(50, 50);
                    await user.FlyMapAsync(map.Identity, targetPos.X, targetPos.Y);
                    break;
                }

                case FamilyPromptType.ClaimExperience:
                {
                    if (user.Family == null)
                        return;

                    if (war.IsInTime)
                        return;

                    DynamicNpc npc = war.GetDominatingNpc(user.Family);
                    if (npc == null)
                        return;

                    GameMap map = MapManager.GetMap((uint) npc.Data1);
                    if (map == null) return;

                    if ((DateTime.Now - user.FamilyMember.JoinDate).TotalDays < 1)
                    {
                        Action = FamilyPromptType.CannotClaim;
                        await user.SendAsync(this);
                        return;
                    }

                    if (!war.HasExpToClaim(user))
                    {
                        Action = FamilyPromptType.WrongExpClaimTime;
                        await user.SendAsync(this);
                        return;
                    }

                    if (user.Level >= Role.MAX_UPLEV)
                    {
                        Action = FamilyPromptType.ReachedMaxLevel;
                        await user.SendAsync(this);
                        return;
                    }

                    double exp = war.GetNextExpReward(user);

                    if (exp == 0)
                        return;

                    DbLevelExperience currLevExp = ExperienceManager.GetLevelExperience(user.Level);
                    if (currLevExp == null)
                        return;

                    await RoleManager.BroadcastMsgAsync(
                        string.Format(Language.StrFetchFamilyNpcExpSuccess, user.Name, map.Name, user.Level, exp * 100),
                        TalkChannel.Center);

                    var awardExp = (long) (currLevExp.Exp * exp);
                    await user.AwardExperienceAsync(awardExp);
                    await war.SetExpRewardAwardedAsync(user);
                    break;
                }

                case FamilyPromptType.ClaimRevenue:
                {
                    if (user.Family == null)
                        return;

                    if (war.IsInTime)
                        return;

                    if (user.FamilyPosition != Family.FamilyRank.ClanLeader)
                        return;

                    DynamicNpc npc = war.GetDominatingNpc(user.Family);
                    if (npc == null)
                        return;

                    GameMap map = MapManager.GetMap((uint) npc.Data1);
                    if (map == null) return;

                    if (!war.HasRewardToClaim(user))
                    {
                        Action = DateTime.Now.Hour >= 21
                                     ? FamilyPromptType.ClaimedAlready
                                     : FamilyPromptType.ClaimOnceADay;
                        await user.SendAsync(this);
                        return;
                    }

                    if (!user.UserPackage.IsPackSpare(5))
                    {
                        await user.SendAsync(string.Format(Language.StrNotEnoughSpaceN, 5), TalkChannel.TopLeft,
                                             Color.Red);
                        return;
                    }

                    uint idItem = war.GetNextReward(user, RequestNpc);
                    if (idItem == 0)
                        return;

                    await war.SetRewardAwardedAsync(user);

                    await user.UserPackage.AwardItemAsync(idItem);
                    await user.Family.SendAsync(
                        string.Format(Language.StrFetchFamilyNpcIncomeSuccess, user.Name, map.Name));
                    break;
                }

                default:
                {
                    if (client.Character.IsPm())
                        await client.SendAsync(new MsgTalk(client.Identity, TalkChannel.Service,
                                                           $"Missing packet {Type}, Action {Action}, Length {Length}"));

                    await Log.WriteLogAsync(LogLevel.Warning,
                                            "Missing packet {0}, Action {1}, Length {2}\n{3}", Type, Action, Length,
                                            PacketDump.Hex(Encode()));
                    break;
                }
            }
        }
    }
}