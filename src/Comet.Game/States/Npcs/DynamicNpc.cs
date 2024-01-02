using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Packets;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.States.Npcs
{
    public sealed class DynamicNpc : BaseNpc
    {
        private readonly DbDynanpc m_dbNpc;
        private readonly ConcurrentDictionary<uint, Score> m_dicScores = new();
        private readonly TimeOut m_Death = new(5);

        public DynamicNpc(DbDynanpc npc)
            : base(npc.Id)
        {
            m_dbNpc = npc;

            mIdMap = npc.Mapid;
            mPosX = npc.Cellx;
            mPosY = npc.Celly;

            Name = npc.Name;

            if (IsSynFlag() && OwnerIdentity > 0)
            {
                Syndicate syn = SyndicateManager.GetSyndicate((int) OwnerIdentity);
                if (syn != null)
                    Name = syn.Name;
            }
        }

        #region Socket

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgNpcInfoEx(this));
        }

        #endregion

        #region Type

        public override uint Mesh
        {
            get => m_dbNpc.Lookface;
            set => m_dbNpc.Lookface = (ushort) value;
        }

        public override ushort Type => m_dbNpc.Type;

        public void SetType(ushort type)
        {
            m_dbNpc.Type = type;
        }

        public override NpcSort Sort => (NpcSort) m_dbNpc.Sort;

        public override int Base => (int) m_dbNpc.Base;

        public void SetSort(ushort sort)
        {
            m_dbNpc.Sort = sort;
        }

        public override uint OwnerType
        {
            get => m_dbNpc.OwnerType;
            set => m_dbNpc.OwnerType = value;
        }

        public override uint OwnerIdentity
        {
            get => m_dbNpc.Ownerid;
            set => m_dbNpc.Ownerid = value;
        }

        #endregion

        #region Life

        public override uint Life
        {
            get => m_dbNpc.Life;
            set => m_dbNpc.Life = value;
        }

        public override uint MaxLife => m_dbNpc.Maxlife;

        #endregion

        #region Position

        public override async Task<bool> ChangePosAsync(uint idMap, ushort x, ushort y)
        {
            if (await base.ChangePosAsync(idMap, x, y))
            {
                m_dbNpc.Mapid = idMap;
                m_dbNpc.Celly = y;
                m_dbNpc.Cellx = x;
                await SaveAsync();
                return true;
            }

            return false;
        }

        #endregion

        #region Attributes

        public override async Task<bool> AddAttributesAsync(ClientUpdateType type, long value)
        {
            return await base.AddAttributesAsync(type, value) && await SaveAsync();
        }

        public override async Task<bool> SetAttributesAsync(ClientUpdateType type, ulong value)
        {
            switch (type)
            {
                case ClientUpdateType.Mesh:
                    Mesh = (uint) value;
                    await BroadcastRoomMsgAsync(new MsgNpcInfoEx(this), false);
                    return await SaveAsync();

                case ClientUpdateType.Hitpoints:
                    m_dbNpc.Life = Math.Min((uint) value, MaxLife);
                    await BroadcastRoomMsgAsync(new MsgUserAttrib(Identity, ClientUpdateType.Hitpoints, Life), false);
                    return await SaveAsync();

                case ClientUpdateType.MaxHitpoints:
                    m_dbNpc.Maxlife = (uint) value;
                    await BroadcastRoomMsgAsync(new MsgNpcInfoEx(this), false);
                    return await SaveAsync();
            }

            return await base.SetAttributesAsync(type, value) && await SaveAsync();
        }

        public bool IsGoal()
        {
            return Type == WEAPONGOAL_NPC || Type == MAGICGOAL_NPC;
        }

        public bool IsCityGate()
        {
            return Type == ROLE_CITY_GATE_NPC;
        }

        #endregion

        #region Task and Data

        public uint LinkId
        {
            get => m_dbNpc.Linkid;
            set => m_dbNpc.Linkid = value;
        }

        public void SetTask(int id, uint task)
        {
            switch (id)
            {
                case 0:
                    m_dbNpc.Task0 = task;
                    break;
                case 1:
                    m_dbNpc.Task1 = task;
                    break;
                case 2:
                    m_dbNpc.Task2 = task;
                    break;
                case 3:
                    m_dbNpc.Task3 = task;
                    break;
                case 4:
                    m_dbNpc.Task4 = task;
                    break;
                case 5:
                    m_dbNpc.Task5 = task;
                    break;
                case 6:
                    m_dbNpc.Task6 = task;
                    break;
                case 7:
                    m_dbNpc.Task7 = task;
                    break;
            }
        }

        public override uint Task0 => m_dbNpc.Task0;
        public override uint Task1 => m_dbNpc.Task1;
        public override uint Task2 => m_dbNpc.Task2;
        public override uint Task3 => m_dbNpc.Task3;
        public override uint Task4 => m_dbNpc.Task4;
        public override uint Task5 => m_dbNpc.Task5;
        public override uint Task6 => m_dbNpc.Task6;
        public override uint Task7 => m_dbNpc.Task7;

        public override int Data0
        {
            get => m_dbNpc.Data0;
            set => m_dbNpc.Data0 = value;
        }

        public override int Data1
        {
            get => m_dbNpc.Data1;
            set => m_dbNpc.Data1 = value;
        }

        public override int Data2
        {
            get => m_dbNpc.Data2;
            set => m_dbNpc.Data2 = value;
        }

        public override int Data3
        {
            get => m_dbNpc.Data3;
            set => m_dbNpc.Data3 = value;
        }

        public override string DataStr
        {
            get => m_dbNpc.Datastr;
            set => m_dbNpc.Datastr = value;
        }

        #endregion

        #region Ownership

        public override async Task DelNpcAsync()
        {
            await SetAttributesAsync(ClientUpdateType.Hitpoints, 0);
            m_Death.Update();

            if (IsSynFlag() || IsCtfFlag())
                await Map.SetStatusAsync(1, false);
            else if (!IsGoal()) await DeleteAsync();

            await LeaveMapAsync();
        }

        public async Task<bool> SetOwnerAsync(uint idOwner, bool withLinkMap = false)
        {
            if (idOwner == 0)
            {
                OwnerIdentity = 0;
                Name = "";

                await BroadcastRoomMsgAsync(new MsgNpcInfoEx(this), false);
                await SaveAsync();
                return true;
            }

            OwnerIdentity = idOwner;
            if (IsSynFlag())
            {
                Syndicate syn = SyndicateManager.GetSyndicate((int) OwnerIdentity);
                if (syn == null)
                {
                    OwnerIdentity = 0;
                    Name = "";
                }
                else
                {
                    Name = syn.Name;
                }
            }

            // TODO
            /*if (withLinkMap)
            {
                foreach (var player in Kernel.RoleManager.QueryUserSetByMap(MapIdentity))
                {

                }
            }*/

            await SaveAsync();
            await BroadcastRoomMsgAsync(new MsgNpcInfoEx(this) {Lookface = m_dbNpc.Lookface}, false);
            return true;
        }

        #endregion

        #region Score

        public async Task CheckFightTimeAsync()
        {
            if (!IsSynFlag())
                return;

            if (Data1 == 0 || Data2 == 0)
                return;

            var strNow = "";
            DateTime now = DateTime.Now;
            strNow += (now.DayOfWeek == 0 ? 7 : (int) now.DayOfWeek).ToString(CultureInfo.InvariantCulture);
            strNow += now.Hour.ToString("00");
            strNow += now.Minute.ToString("00");
            strNow += now.Second.ToString("00");

            int now0 = int.Parse(strNow);
            if (now0 < Data1 || now0 >= Data2)
            {
                if (Map.IsWarTime())
                    await OnFightEndAsync();
                return;
            }

            if (!Map.IsWarTime())
            {
                await Map.SetStatusAsync(1, true);
                await Map.BroadcastMsgAsync(Language.StrWarStart, TalkChannel.System);
            }
        }

        public async Task OnFightEndAsync()
        {
            await Map.SetStatusAsync(1, false);
            await Map.BroadcastMsgAsync(Language.StrWarEnd, TalkChannel.System);
            Map.ResetBattle();
        }

        public async Task BroadcastRankingAsync()
        {
            if (!IsSynFlag() || !IsAttackable(null) || m_dicScores.Count == 0)
                return;

            await Map.BroadcastMsgAsync(Language.StrWarRankingStart, TalkChannel.GuildWarRight1);
            var i = 0;
            foreach (Score score in m_dicScores.Values.OrderByDescending(x => x.Points))
            {
                if (i++ >= 5)
                    break;

                await Map.BroadcastMsgAsync(string.Format(Language.StrWarRankingNo, i, score.Name, score.Points),
                                            TalkChannel.GuildWarRight2);
            }
        }

        public void AddSynWarScore(Syndicate syn, long score)
        {
            if (syn == null)
                return;

            if (!m_dicScores.ContainsKey(syn.Identity))
                m_dicScores.TryAdd(syn.Identity, new Score(syn.Identity, syn.Name));

            m_dicScores[syn.Identity].Points += score;
        }

        public Score GetTopScore()
        {
            return m_dicScores.Values.OrderByDescending(x => x.Points).ThenBy(x => x.Identity).FirstOrDefault();
        }

        public void ClearScores()
        {
            m_dicScores.Clear();
        }

        #endregion

        #region Battle

        public override async Task<bool> BeAttackAsync(BattleSystem.MagicType magic, Role attacker, int nPower,
                                                       bool bReflectEnable)
        {
            var decreaseLife = (int) Calculations.CutOverflow(Life, nPower);
            await AddAttributesAsync(ClientUpdateType.Hitpoints, decreaseLife * -1);
            if (IsSynNpc() && OwnerIdentity != 0)
            {
                Syndicate syn = SyndicateManager.GetSyndicate(OwnerIdentity);
                if (syn != null && attacker is Character user)
                    if (user.SyndicateIdentity != OwnerIdentity)
                    {
                        //await user.AwardMoneyAsync();
                    }
            }

            if (!IsAlive)
                await BeKillAsync(attacker);

            return true;
        }

        public override Task BeKillAsync(Role attacker)
        {
            if (m_dbNpc.Linkid != 0)
                return GameAction.ExecuteActionAsync(m_dbNpc.Linkid, attacker as Character, this, null, string.Empty);
            return Task.CompletedTask;
        }

        public int GetMaxFixMoney()
        {
            return (int) Calculations.CutRange(Calculations.MulDiv(MaxLife - 1, 1, 1) + 1, 0, MaxLife);
        }

        public int GetLostFixMoney()
        {
            var nLostLifeTmp = (int) (MaxLife - Life);
            return (int) Calculations.CutRange(Calculations.MulDiv(nLostLifeTmp - 1, 1, 1) + 1, 0, MaxLife);
        }

        #endregion

        #region Database

        public override async Task<bool> SaveAsync()
        {
            return await ServerDbContext.SaveAsync(m_dbNpc);
        }

        public override async Task<bool> DeleteAsync()
        {
            return await ServerDbContext.DeleteAsync(m_dbNpc);
        }

        #endregion

        public class Score
        {
            public Score(uint id, string name)
            {
                Identity = id;
                Name = name;
            }

            public uint Identity { get; }
            public string Name { get; }
            public long Points { get; set; }
        }
    }
}