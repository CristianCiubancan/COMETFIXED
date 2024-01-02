using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Packets;
using Comet.Game.States.Families;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.States.Events
{
    public sealed class FamilyWar : GameEvent
    {
        private const uint RewardStcU = 100020;

        private Dictionary<uint, uint[]> mPrizePool { get; } = new()
        {
            {10026, new uint[] {0, 722458, 722457, 722456, 722455, 722454}},
            {10027, new uint[] {0, 722458, 722457, 722456, 722455, 722454}},
            {10028, new uint[] {0, 722478, 722477, 722476, 722475, 722474}},
            {10029, new uint[] {0, 722478, 722477, 722476, 722475, 722474}},
            {10030, new uint[] {0, 722473, 722472, 722471, 722470, 722469}},
            {10031, new uint[] {0, 722473, 722472, 722471, 722470, 722469}},
            {10032, new uint[] {0, 722468, 722467, 722466, 722465, 722464}},
            {10033, new uint[] {0, 722468, 722467, 722466, 722465, 722464}},
            {10034, new uint[] {0, 722463, 722462, 722461, 722460, 722459}},
            {10035, new uint[] {0, 722463, 722462, 722461, 722460, 722459}}
        };

        private Dictionary<uint, uint> mGoldFee { get; } = new()
        {
            {10026, 1000000},
            {10027, 1000000},
            {10028, 200000},
            {10029, 200000},
            {10030, 400000},
            {10031, 400000},
            {10032, 600000},
            {10033, 600000},
            {10034, 800000},
            {10035, 800000}
        };

        private readonly List<double> mExpRewards = new()
        {
            0.01,
            0.015d,
            0.02,
            0.025d,
            0.03,
            0.035d,
            0.05
        };

        private int mLastUpdate;
        private FamilyWarStage mStage = FamilyWarStage.Idle;

        private const string OwnerCity = "data0";
        private const string ChallengeMap = "data1";
        private const string TemporaryWinner = "data2";
        private const string PrizePool = "data3";

        public FamilyWar()
            : base("Clan War")
        {
        }

        #region Override

        public override bool IsInTime => uint.Parse(DateTime.Now.ToString("HHmmss")) >= 203000
                                         && uint.Parse(DateTime.Now.ToString("HHmmss")) < 210000;

        public override bool IsAllowedToJoin(Role sender)
        {
            return uint.Parse(DateTime.Now.ToString("HHmmss")) >= 203000
                   && uint.Parse(DateTime.Now.ToString("HHmmss")) < 203500;
        }

        public override async Task OnTimerAsync()
        {
            int now = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
            int time = int.Parse(DateTime.Now.ToString("HHmm"));
            if (mStage == FamilyWarStage.Idle)
            {
                if (time is >= 2000 and < 2030)
                {
                    await Log.WriteLogAsync("FamilyWarDebug", LogLevel.Debug, "Starting Clan Wars!");
                    foreach (uint npcId in mGoldFee.Keys)
                    {
                        var npc = RoleManager.FindRole<DynamicNpc>(npcId);
                        if (npc == null)
                        {
                            await Log.WriteLogAsync("FamilyWarDebug", LogLevel.Debug,
                                                    $"Could not find NPC {npcId} for clan war startup!");
                            continue;
                        }

                        npc.SetData(TemporaryWinner, int.MaxValue); // temporary winner (if set)
                        await npc.SaveAsync();
                    }

                    mStage = FamilyWarStage.Preparing;
                }
            }
            else if (mStage == FamilyWarStage.Preparing)
            {
                if (time == 2030)
                {
                    foreach (var idNpc in mGoldFee.Keys)
                    {
                        Family owner = GetFamilyOwner(idNpc);
                        if (owner != null)
                        {
                            await owner.SendAsync(new MsgFamilyOccupy
                            {
                                Action = MsgFamilyOccupy<Client>.FamilyPromptType.AnnounceWarBegin
                            });
                        }

                        var npc = RoleManager.GetRole<DynamicNpc>(idNpc);
                        foreach (var challenger in GetChallengersByMap((uint) npc.GetData(ChallengeMap)))
                        {
                            await challenger.SendAsync(new MsgFamilyOccupy
                            {
                                Action = MsgFamilyOccupy<Client>.FamilyPromptType.AnnounceWarBegin
                            });
                        }
                    }

                    mStage = FamilyWarStage.Running;
                }
            }
            else if (mStage == FamilyWarStage.Running)
            {
                if (time == 2045)
                {
                    mStage = FamilyWarStage.WaitingConfirmation;
                    // hm?
                }
            }
            else if (mStage == FamilyWarStage.WaitingConfirmation)
            {
                if (time == 2055 &&
                    mLastUpdate != now)
                {
                    foreach (Family family in FamilyManager.QueryFamilies(x => x.ChallengeMap != 0))
                    {
                        family.ChallengeMap = 0;
                        await family.SaveAsync();
                    }

                    await ApplyWinnersAsync();

                    mLastUpdate = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                    mStage = FamilyWarStage.Idle;
                }
            }
        }

        /// <inheritdoc />
        public override async Task<bool> CreateAsync()
        {
            await ApplyWinnersAsync();
            return true;
        }

        #endregion

        private async Task ApplyWinnersAsync()
        {
            foreach (uint npcId in mGoldFee.Keys)
            {
                var npc = RoleManager.FindRole<DynamicNpc>(npcId);
                if (npc == null)
                {
                    await Log.WriteLogAsync("FamilyWarDebug", LogLevel.Debug,
                                            $"Could not find NPC {npcId} to apply winner!");
                    continue;
                }

                int idWinner = npc.GetData(TemporaryWinner);
                if (idWinner is not 0 and not int.MaxValue)
                {
                    var occupyMap = (uint) npc.GetData(ChallengeMap);
                    Family old = GetFamilyOwner(npcId);
                    if (old != null && old.FamilyMap == occupyMap) // must be occupying this map
                    {
                        old.ChallengeMap = 0;
                        old.FamilyMap = 0;
                        old.OccupyDate = 0;
                        await old.SaveAsync();
                    }

                    Family winner = FamilyManager.GetFamily((uint) idWinner);
                    if (winner != null)
                    {
                        winner.ChallengeMap = 0;
                        winner.FamilyMap = occupyMap;
                        if (winner.OccupyDate == 0)
                            winner.OccupyDate = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
                        await winner.SaveAsync();
                    }
                }

                foreach (Family challenger in GetChallengersByMap((uint) npc.GetData(ChallengeMap)))
                {
                    if (challenger.ChallengeTime <= 0)
                    {
                        challenger.ChallengeMap = 0;
                        challenger.ChallengeTime = 0;
                        continue;
                    }

                    DateTime today = DateTime.Now;
                    var challengeTime = UnixTimestamp.ToDateTime(challenger.ChallengeTime);
                    int timeNow = int.Parse(DateTime.Now.ToString("HHmm"));
                    var reset = false;
                    // If server startup time is day time, we must check for yesterday.
                    if (timeNow < 2000)
                    {
                        DateTime yesterday = today.AddDays(-1);
                        yesterday = new DateTime(yesterday.Year, yesterday.Month, yesterday.Day, 21, 00, 00);

                        if (challengeTime < yesterday)
                            reset = true;
                    }
                    // If server startup past 21:00 then we must check if family challenged before war time.
                    else if (timeNow >= 2100)
                    {
                        var closeTimeToday = new DateTime(today.Year, today.Month, today.Day, 20, 00, 00);

                        if (challengeTime < closeTimeToday)
                            reset = true;
                    }
                    else continue;

                    if (reset)
                    {
                        challenger.ChallengeMap = 0;
                        challenger.ChallengeTime = 0;
                        await challenger.SaveAsync();
                    }
                }

                npc.SetData(TemporaryWinner, 0); // temporary winner (if set)
                await npc.SaveAsync();
            }
        }

        private int GetExpRewardIdx(uint occupyDays)
        {
            occupyDays = Math.Max(1, occupyDays);
            return (int) ((occupyDays - 1) % mExpRewards.Count);
        }

        public uint GetNextReward(Character sender, uint idNpc = 0)
        {
            DynamicNpc npc;
            if (idNpc != 0)
            {
                npc = RoleManager.FindRole<DynamicNpc>(idNpc);
                if (npc == null || !mPrizePool.ContainsKey(idNpc))
                    return 0;

                if (sender.FamilyIdentity == 0 || sender.Family.FamilyMap == 0 || sender.Family.FamilyMap != npc.Data1)
                    return mPrizePool[npc.Identity][1];

                return mPrizePool[idNpc][sender.Family.Rank];
            }

            if (sender.Family == null)
                return 0;

            // I want the ID of my next reward. This means that I'll get the reward to my Family Map.
            npc = RoleManager.FindRole<DynamicNpc>(x => x.Data1 == sender.Family.FamilyMap);
            if (npc == null || !mPrizePool.ContainsKey(npc.Identity))
                return 0;

            return mPrizePool[npc.Identity][sender.Family.Rank];
        }

        public uint GetNextWeekReward(Character sender, uint idNpc)
        {
            if (idNpc == 0)
                return 0;

            var npc = RoleManager.FindRole<DynamicNpc>(idNpc);
            if (npc == null)
                return 0;

            if (sender.Family == null || sender.Family.FamilyMap == 0 || sender.Family.FamilyMap != npc.Data1)
                return mPrizePool[npc.Identity][sender.Family?.Rank ?? 1];

            return mPrizePool[npc.Identity][sender.Family.Rank];
        }

        public Family GetFamilyOwner(uint idNpc)
        {
            var npc = RoleManager.FindRole<DynamicNpc>(idNpc);
            if (npc == null)
                return null;
            return FamilyManager.GetOccupyOwner((uint) npc.Data1);
        }

        public DynamicNpc GetChallengeNpc(Family family)
        {
            if (family == null || family.ChallengeMap == 0)
                return null;
            return RoleManager.FindRole<DynamicNpc>(x => x.Type == BaseNpc.ROLE_FAMILY_WAR_FLAG &&
                                                         x.Data1 == family.ChallengeMap);
        }

        public DynamicNpc GetDominatingNpc(Family family)
        {
            if (family == null || family.FamilyMap == 0)
                return null;
            return RoleManager.FindRole<DynamicNpc>(x => x.Type == BaseNpc.ROLE_FAMILY_WAR_FLAG &&
                                                         x.Data1 == family.FamilyMap);
        }

        public uint GetGoldFee(uint idNpc)
        {
            if (!mGoldFee.ContainsKey(idNpc))
                return 0;
            return mGoldFee[idNpc];
        }

        public GameMap GetMap(uint idNpc)
        {
            return MapManager.GetMap(idNpc);
        }

        public List<Family> GetChallengersByMap(uint idMap)
        {
            return FamilyManager.QueryFamilies(x => x.ChallengeMap == idMap);
        }

        public bool IsChallenged(uint idMap)
        {
            if (idMap == 0)
                return false;

            List<Family> challengers = GetChallengersByMap(idMap);
            return challengers is {Count: > 0};
        }

        private bool ValidateRewardTime(DateTime time)
        {
            DateTime now = DateTime.Now;
            if (now.Year != time.Year)
                return true;

            uint nowTime = uint.Parse(now.ToString("HHmmss"));
            uint lastTime = uint.Parse(now.ToString("HHmmss"));
            if (lastTime is >= 210000 and <= 235959)
            {
                if (nowTime is >= 210000 and <= 235959 && now.DayOfYear != time.DayOfYear)
                    return true;

                if (nowTime < 203000)
                    return true;
            }
            else if (lastTime <= 202959)
            {
                if (nowTime <= 202959 && now.DayOfYear != time.DayOfYear)
                    return true;

                if (nowTime >= 210000)
                    return true;
            }

            // may be error - must fix manually
            return false;
        }

        public bool HasExpToClaim(Character user)
        {
            if (IsInTime)
                return false;

            if (user?.Family == null)
                return false;

            //int daysSinceCreation = (int) (DateTime.Now - user.Family.CreationDate).TotalDays;
            //int daysSinceJoin = (int)(DateTime.Now - user.FamilyMember.JoinDate).TotalDays;
            //if (daysSinceCreation > 0 && daysSinceJoin < 1)
            //    return false;

            DynamicNpc npc = GetDominatingNpc(user.Family);
            if (npc == null)
                return false;

            DateTime? last = user.Statistic.GetStc(RewardStcU)?.Timestamp;
            if (last.HasValue)
                return ValidateRewardTime(last.Value);
            return true;
        }

        public async Task SetExpRewardAwardedAsync(Character user)
        {
            DbStatistic currStc = user.Statistic.GetStc(RewardStcU);
            if (currStc == null)
            {
                if (!await user.Statistic.AddOrUpdateAsync(RewardStcU, 0, 0, true))
                    return;

                currStc = user.Statistic.GetStc(RewardStcU);
                if (currStc == null)
                    return;
            }

            await user.Statistic.AddOrUpdateAsync(RewardStcU, 0, currStc.Data + 1, true);
        }

        public double GetNextExpReward(Character user)
        {
            if (!HasExpToClaim(user))
                return 0;
            return mExpRewards[GetExpRewardIdx(user.Family.OccupyDays)];
        }

        public bool HasRewardToClaim(Character user)
        {
            if (IsInTime)
                return false;

            if (user?.Family == null)
                return false;

            DynamicNpc npc = GetDominatingNpc(user.Family);
            if (npc == null)
                return false;

            if (DateTime.TryParseExact(npc.DataStr, "O", Thread.CurrentThread.CurrentCulture,
                                       DateTimeStyles.AssumeLocal, out DateTime date) && !ValidateRewardTime(date))
                return false;
            return true;
        }

        public async Task SetRewardAwardedAsync(Character user)
        {
            DynamicNpc npc = GetDominatingNpc(user.Family);
            if (npc == null)
                return;

            DbStatistic currStc = user.Statistic.GetStc(RewardStcU, 1);
            if (currStc == null)
            {
                if (!await user.Statistic.AddOrUpdateAsync(RewardStcU, 1, 0, true))
                    return;

                currStc = user.Statistic.GetStc(RewardStcU, 1);
                if (currStc == null)
                    return;
            }

            npc.DataStr = DateTime.Now.ToString("O");
            await npc.SaveAsync();
            await user.Statistic.AddOrUpdateAsync(RewardStcU, 1, currStc.Data + 1, true);
        }

        public async Task<bool> ValidateResultAsync(Character user, uint idNpc)
        {
            var npc = RoleManager.FindRole<DynamicNpc>(x => x.Identity == idNpc);
            if (npc == null)
                return false;

            if (npc.GetData(TemporaryWinner) is not 0 and not int.MaxValue)
                return true;

            if (npc.Data1 != user.Family.ChallengeMap && npc.Data1 != user.Family.FamilyMap)
                return false;

            if (IsInTime)
                return false;

            GameMap map = MapManager.GetMap((uint) npc.Data1);
            if (map == null)
                return false;

            var families = new Dictionary<uint, Family>();
            foreach (Character player in map.QueryPlayers(x =>
                                                              x.FamilyIdentity != 0 &&
                                                              (x.Family.FamilyMap == map.Identity ||
                                                               x.Family.ChallengeMap == map.Identity) && x.IsAlive))
                if (!families.ContainsKey(player.FamilyIdentity))
                    families.Add(player.FamilyIdentity, player.Family);

            uint currentTime = uint.Parse(DateTime.Now.ToString("HHmmss"));
            if (families.Count == 1)
            {
                if (currentTime is < 203000 or > 205459)
                    return false;

                Family winner = families.Values.FirstOrDefault();
                if (winner != null && (winner.ChallengeMap != npc.Data1 || winner.FamilyMap != npc.Data1))
                {
                    npc.SetData(TemporaryWinner, (int) winner.Identity);
                    await npc.SaveAsync();
                }
            }
            else if (families.Count > 1)
            {
                if (currentTime is < 204500 or > 205459)
                    return false;

                Family current = FamilyManager.GetOccupyOwner((uint) npc.Data1);
                if (families.All(x => x.Key != current?.Identity))
                {
                    var bpDict = new Dictionary<uint, int>();
                    foreach (Character player in map.QueryPlayers(x =>
                                                                      x.FamilyIdentity != 0 &&
                                                                      (x.Family.FamilyMap == map.Identity ||
                                                                       x.Family.ChallengeMap == map.Identity) &&
                                                                      x.IsAlive))
                    {
                        if (player.FamilyIdentity == 0)
                            continue;

                        if (bpDict.ContainsKey(player.FamilyIdentity))
                            bpDict[player.FamilyIdentity] += player.BattlePower;
                        else
                            bpDict.Add(player.FamilyIdentity, player.BattlePower);
                    }

                    Family winner =
                        FamilyManager.GetFamily(bpDict.OrderByDescending(x => x.Value).FirstOrDefault().Key);

                    npc.SetData(TemporaryWinner, (int) winner.Identity);
                    await npc.SaveAsync();
                }
                else
                {
                    npc.SetData(TemporaryWinner, (int) current.Identity);
                    await npc.SaveAsync();
                    // let's renew the champion
                }
                // return true even if false because the winner is the clan whose is already dominating. wont change
            }
            else
            {
                return false;
            }

            return true;
        }

        private enum FamilyWarStage
        {
            Idle,
            Preparing,
            Running,
            WaitingConfirmation
        }
    }
}