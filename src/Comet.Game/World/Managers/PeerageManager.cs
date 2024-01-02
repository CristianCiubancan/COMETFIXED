using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Network.Packets.Game;
using static Comet.Network.Packets.Game.MsgPeerage<Comet.Game.States.Client>;

namespace Comet.Game.World.Managers
{
    public static class PeerageManager
    {
        private static readonly ConcurrentDictionary<uint, DbPeerage> PeerageSet = new();

        public static async Task InitializeAsync()
        {
            List<DbPeerage> dbPeerages = await PeerageRepository.GetAsync();
            foreach (DbPeerage peerage in dbPeerages) PeerageSet.TryAdd(peerage.UserIdentity, peerage);
        }

        public static async Task DonateAsync(Character user, ulong amount)
        {
            int oldPosition = GetPosition(user.Identity);
            NobilityRank oldRank = GetRanking(user.Identity);

            if (!PeerageSet.TryGetValue(user.Identity, out DbPeerage peerage))
            {
                peerage = new DbPeerage
                {
                    UserIdentity = user.Identity,
                    Name = user.Name,
                    Donation = user.NobilityDonation + amount,
                    FirstDonation = DateTime.Now
                };
                await SaveAsync(peerage);

                PeerageSet.TryAdd(user.Identity, peerage);
            }
            else
            {
                peerage.Donation += amount;
                await SaveAsync(peerage);
            }

            user.NobilityDonation = peerage.Donation;

            NobilityRank rank = GetRanking(user.Identity);
            int position = GetPosition(user.Identity);

            await user.SendNobilityInfoAsync();

            if (position != oldPosition && position < 50)
                foreach (DbPeerage peer in PeerageSet.Values.OrderByDescending(z => z.Donation)
                                                     .ThenBy(y => y.FirstDonation))
                {
                    Character targetUser = RoleManager.GetUser(peer.UserIdentity);
                    if (targetUser != null)
                        await targetUser.SendNobilityInfoAsync(true);
                }

            if (rank != oldRank)
            {
                var message = "";
                switch (rank)
                {
                    case NobilityRank.King:
                        if (user.Gender == 1)
                            message = string.Format(Language.StrPeeragePromptKing, user.Name,
                                                    Kernel.GameConfiguration.ServerName);
                        else
                            message = string.Format(Language.StrPeeragePromptQueen, user.Name,
                                                    Kernel.GameConfiguration.ServerName);
                        break;
                    case NobilityRank.Prince:
                        if (user.Gender == 1)
                            message = string.Format(Language.StrPeeragePromptPrince, user.Name,
                                                    Kernel.GameConfiguration.ServerName);
                        else
                            message = string.Format(Language.StrPeeragePromptPrincess, user.Name,
                                                    Kernel.GameConfiguration.ServerName);
                        break;
                    case NobilityRank.Duke:
                        if (user.Gender == 1)
                            message = string.Format(Language.StrPeeragePromptDuke, user.Name,
                                                    Kernel.GameConfiguration.ServerName);
                        else
                            message = string.Format(Language.StrPeeragePromptDuchess, user.Name,
                                                    Kernel.GameConfiguration.ServerName);
                        break;
                    case NobilityRank.Earl:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptEarl, user.Name);
                        else message = string.Format(Language.StrPeeragePromptCountess, user.Name);
                        break;
                    case NobilityRank.Baron:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptBaron, user.Name);
                        else message = string.Format(Language.StrPeeragePromptBaroness, user.Name);
                        break;
                    case NobilityRank.Knight:
                        if (user.Gender == 1) message = string.Format(Language.StrPeeragePromptKnight, user.Name);
                        else message = string.Format(Language.StrPeeragePromptLady, user.Name);
                        break;
                }

                if (user.Team != null)
                    await user.Team.SyncFamilyBattlePowerAsync();

                if (user.ApprenticeCount > 0)
                    await user.SynchroApprenticesSharedBattlePowerAsync();

                await RoleManager.BroadcastMsgAsync(message, TalkChannel.Center, Color.Red);
            }
        }

        public static NobilityRank GetRanking(uint idUser)
        {
            int position = GetPosition(idUser);
            if (position >= 0 && position < 3)
                return NobilityRank.King;
            if (position >= 3 && position < 15)
                return NobilityRank.Prince;
            if (position >= 15 && position < 50)
                return NobilityRank.Duke;

            DbPeerage peerageUser = GetUser(idUser);
            ulong donation = 0;
            if (peerageUser != null)
            {
                donation = peerageUser.Donation;
            }
            else
            {
                Character user = RoleManager.GetUser(idUser);
                if (user != null) donation = user.NobilityDonation;
            }

            if (donation >= 200000000)
                return NobilityRank.Earl;
            if (donation >= 100000000)
                return NobilityRank.Baron;
            if (donation >= 30000000)
                return NobilityRank.Knight;
            return NobilityRank.Serf;
        }

        public static int GetPosition(uint idUser)
        {
            var found = false;
            int idx = -1;

            foreach (DbPeerage peerage in PeerageSet.Values.OrderByDescending(x => x.Donation)
                                                    .ThenBy(x => x.FirstDonation))
            {
                idx++;
                if (peerage.UserIdentity == idUser)
                {
                    found = true;
                    break;
                }

                if (idx >= 50)
                    break;
            }

            return found ? idx : -1;
        }

        public static async Task SendRankingAsync(Character target, int page)
        {
            if (target == null)
                return;

            const int MAX_PER_PAGE_I = 10;
            const int MAX_PAGES = 5;

            int currentPagesNum = Math.Max(1, Math.Min(PeerageSet.Count / MAX_PER_PAGE_I + 1, MAX_PAGES));
            if (page >= currentPagesNum)
                return;

            var current = 0;
            int min = page * MAX_PER_PAGE_I;
            int max = page * MAX_PER_PAGE_I + MAX_PER_PAGE_I;

            var rank = new List<NobilityStruct>();
            foreach (DbPeerage peerage in PeerageSet.Values.OrderByDescending(x => x.Donation)
                                                    .ThenBy(x => x.FirstDonation))
            {
                if (current >= MAX_PAGES * MAX_PER_PAGE_I)
                    break;

                if (current < min)
                {
                    current++;
                    continue;
                }

                if (current >= max)
                    break;

                Character peerageUser = RoleManager.GetUser(peerage.UserIdentity);
                uint lookface = peerageUser?.Mesh ?? 0;
                rank.Add(new NobilityStruct
                {
                    Identity = peerage.UserIdentity,
                    Name = peerage.Name,
                    Donation = peerage.Donation,
                    LookFace = lookface,
                    Position = current,
                    Rank = GetRanking(peerage.UserIdentity)
                });

                current++;
            }

            var msg = new MsgPeerage(NobilityAction.List, (ushort) Math.Min(MAX_PER_PAGE_I, rank.Count),
                                     (ushort) currentPagesNum);
            msg.Rank.AddRange(rank);
            await target.SendAsync(msg);
        }

        public static DbPeerage GetUser(uint idUser)
        {
            return PeerageSet.TryGetValue(idUser, out DbPeerage peerage) ? peerage : null;
        }

        public static ulong GetNextRankSilver(NobilityRank rank, ulong donation)
        {
            switch (rank)
            {
                case NobilityRank.Knight: return 30000000 - donation;
                case NobilityRank.Baron:  return 100000000 - donation;
                case NobilityRank.Earl:   return 200000000 - donation;
                case NobilityRank.Duke:   return GetDonation(50) - donation;
                case NobilityRank.Prince: return GetDonation(15) - donation;
                case NobilityRank.King:   return GetDonation(3) - donation;
                default:                  return 0;
            }
        }

        public static ulong GetDonation(int position)
        {
            var ranking = 1;
            ulong donation = 0;
            foreach (DbPeerage peerage in PeerageSet.Values.OrderByDescending(x => x.Donation)
                                                    .ThenBy(x => x.FirstDonation))
            {
                donation = peerage.Donation;
                if (ranking++ == position)
                    break;
            }

            return Math.Max(3000000, donation);
        }

        public static async Task SaveAsync()
        {
            foreach (DbPeerage peerage in PeerageSet.Values)
                await SaveAsync(peerage);
        }

        public static Task SaveAsync(DbPeerage peerage)
        {
            return ServerDbContext.SaveAsync(peerage);
        }
    }
}