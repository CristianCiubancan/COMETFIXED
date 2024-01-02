using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Packets;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;

namespace Comet.Game.States
{
    public sealed class Team
    {
        public const int MAX_MEMBERS = 5;

        private readonly ConcurrentDictionary<uint, Character> m_dicPlayers = new();

        public Team(Character leader)
        {
            Leader = leader;
            JoinEnable = true;
            MoneyEnable = true;
        }

        public Character Leader { get; }

        public bool JoinEnable { get; set; }
        public bool MoneyEnable { get; set; }
        public bool ItemEnable { get; set; }
        public bool JewelEnable { get; set; }

        public ICollection<Character> Members => m_dicPlayers.Values;

        public int MemberCount => m_dicPlayers.Count;


        public bool Create()
        {
            if (Leader.Team != null)
                return false;

            m_dicPlayers.TryAdd(Leader.Identity, Leader);
            Leader.Team = this;
            return true;
        }

        /// <summary>
        ///     Erase the team.
        /// </summary>
        public async Task<bool> DismissAsync(Character request, bool disconnect = false)
        {
            if (request.Identity != Leader.Identity)
            {
                await request.SendAsync(Language.StrTeamDismissNoLeader);
                return false;
            }

            await SendAsync(new MsgTeam
            {
                Action = MsgTeam<Client>.TeamAction.Dismiss,
                Identity = Leader.Identity
            }, disconnect ? request.Identity : 0);

            foreach (Character member in m_dicPlayers.Values) member.Team = null;

            return true;
        }

        public async Task<bool> DismissMemberAsync(Character user)
        {
            if (!m_dicPlayers.TryRemove(user.Identity, out Character target))
                return false;

            await SendAsync(new MsgTeam
            {
                Identity = user.Identity,
                Action = MsgTeam<Client>.TeamAction.LeaveTeam
            });
            user.Team = null;

            await SyncFamilyBattlePowerAsync();
            return true;
        }

        public async Task<bool> KickMemberAsync(Character leader, uint idTarget)
        {
            if (!IsLeader(leader.Identity) || !m_dicPlayers.TryGetValue(idTarget, out Character target))
                return false;

            await SendAsync(new MsgTeam
            {
                Identity = idTarget,
                Action = MsgTeam<Client>.TeamAction.Kick
            });

            m_dicPlayers.TryRemove(idTarget, out _);
            target.Team = null;

            await SyncFamilyBattlePowerAsync();
            return true;
        }

        public async Task<bool> EnterTeamAsync(Character target)
        {
            if (!m_dicPlayers.TryAdd(target.Identity, target))
                return false;

            target.Team = this;
            await SendShowAsync(target);

            await target.SendAsync(string.Format(Language.StrPickupSilvers,
                                                 MoneyEnable ? Language.StrOpen : Language.StrClose));
            await target.SendAsync(string.Format(Language.StrTeamItems,
                                                 ItemEnable ? Language.StrOpen : Language.StrClose));
            await target.SendAsync(string.Format(Language.StrTeamGems,
                                                 JewelEnable ? Language.StrOpen : Language.StrClose));

            await SyncFamilyBattlePowerAsync();
            return true;
        }

        public bool IsLeader(uint id)
        {
            return Leader.Identity == id;
        }

        public bool IsMember(uint id)
        {
            return m_dicPlayers.ContainsKey(id);
        }

        public async Task SendShowAsync(Character target)
        {
            await target.SendAsync(new MsgTeamMember
            {
                Action = MsgTeamMember.ADD_MEMBER_B,
                Members = new List<MsgTeamMember<Client>.TeamMember>
                {
                    new()
                    {
                        Identity = Leader.Identity,
                        Name = Leader.Name,
                        MaxLife = (ushort) Leader.MaxLife,
                        Life = (ushort) Leader.Life,
                        Lookface = Leader.Mesh
                    }
                }
            });

            await target.SendAsync(new MsgTeamMember
            {
                Action = MsgTeamMember.ADD_MEMBER_B,
                Members = new List<MsgTeamMember<Client>.TeamMember>
                {
                    new()
                    {
                        Identity = target.Identity,
                        Name = target.Name,
                        MaxLife = (ushort) target.MaxLife,
                        Life = (ushort) target.Life,
                        Lookface = target.Mesh
                    }
                }
            });

            foreach (Character member in m_dicPlayers.Values)
            {
                await member.SendAsync(new MsgTeamMember
                {
                    Action = MsgTeamMember.ADD_MEMBER_B,
                    Members = new List<MsgTeamMember<Client>.TeamMember>
                    {
                        new()
                        {
                            Identity = target.Identity,
                            Name = target.Name,
                            MaxLife = (ushort) target.MaxLife,
                            Life = (ushort) target.Life,
                            Lookface = target.Mesh
                        }
                    }
                });

                if (target.Identity != member.Identity)
                    await target.SendAsync(new MsgTeamMember
                    {
                        Action = MsgTeamMember.ADD_MEMBER_B,
                        Members = new List<MsgTeamMember<Client>.TeamMember>
                        {
                            new()
                            {
                                Identity = member.Identity,
                                Name = member.Name,
                                MaxLife = (ushort) member.MaxLife,
                                Life = (ushort) member.Life,
                                Lookface = member.Mesh
                            }
                        }
                    });
            }
        }

        public async Task SendAsync(IPacket msg, uint exclude = 0)
        {
            foreach (Character player in m_dicPlayers.Values)
            {
                if (exclude == player.Identity)
                    continue;
                await player.SendAsync(msg);
            }
        }

        public async Task BroadcastMemberLifeAsync(Character user, bool maxLife = false)
        {
            if (user == null || !IsMember(user.Identity))
                return;

            var msg = new MsgUserAttrib(user.Identity, ClientUpdateType.Hitpoints, user.Life);
            if (maxLife)
                msg.Append(ClientUpdateType.MaxHitpoints, user.MaxLife);

            foreach (Character member in m_dicPlayers.Values)
                if (member.Identity != user.Identity)
                    await member.SendAsync(msg);
        }

        public async Task AwardMemberExpAsync(uint idKiller, Role target, long exp)
        {
            if (target == null || exp == 0)
                return;

            if (!m_dicPlayers.TryGetValue(idKiller, out Character killer))
                return;

            foreach (Character user in m_dicPlayers.Values)
            {
                if (user.Identity == idKiller)
                    continue;

                if (!user.IsAlive)
                    continue;

                if (user.MapIdentity != killer.MapIdentity)
                    continue;

                if (user.GetDistance(killer) > Screen.VIEW_SIZE * 2)
                    continue;

                DbLevelExperience dbExp = ExperienceManager.GetLevelExperience(user.Level);
                if (dbExp == null)
                    continue;

                long addExp = user.AdjustExperience(target, exp, false);
                addExp = (long) Math.Min(dbExp.Exp, (ulong) addExp);
                addExp = Math.Max(1, Math.Min(user.Level * 360, addExp));

                addExp = (int) Math.Min(addExp, user.Level * 360);

                if (user.IsMate(killer) || user.IsApprentice(idKiller))
                    addExp *= 2;

                await user.AwardBattleExpAsync(addExp, true);
                await user.SendAsync(string.Format(Language.StrTeamExperience, addExp));
            }
        }

        public int FamilyBattlePower(Character user, out uint idProvider)
        {
            idProvider = 0;
            if (!m_dicPlayers.ContainsKey(user.Identity))
                return 0;

            if (user.FamilyIdentity == 0)
                return 0;

            Character clanMember = m_dicPlayers.Values
                                               .OrderByDescending(x => x.PureBattlePower)
                                               .FirstOrDefault(
                                                   x => x.Identity != user.Identity &&
                                                        x.MapIdentity == user.MapIdentity &&
                                                        x.FamilyIdentity == user.FamilyIdentity);

            if (clanMember == null || clanMember.PureBattlePower <= user.PureBattlePower)
                return 0;

            DbFamilyBattleEffectShareLimit limit = FamilyManager.GetSharedBattlePowerLimit(user.PureBattlePower);
            if (limit == null)
                return 0;

            idProvider = clanMember.Identity;
            var value = (int) ((clanMember.PureBattlePower - user.PureBattlePower) *
                               (user.Family.SharedBattlePowerFactor / 100d));
            value = Math.Min(Math.Max(0, value), limit.ShareLimit);
            return value;
        }

        public async Task SyncFamilyBattlePowerAsync()
        {
            foreach (Character member in m_dicPlayers.Values) await member.SynchroFamilyBattlePowerAsync();
        }
    }
}