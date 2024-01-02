using System;
using System.Drawing;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Internal.AI;
using Comet.Game.Packets;
using Comet.Game.Packets.Ai;
using Comet.Game.States.Items;
using Comet.Game.World;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Ai;
using Comet.Network.Packets.Game;
using Comet.Shared;
using static Comet.Game.States.Items.MapItem;
using MsgAction = Comet.Game.Packets.MsgAction;
using MsgInteract = Comet.Game.Packets.MsgInteract;

namespace Comet.Game.States
{
    public class Monster : Role
    {
        private readonly DbMonstertype mDbMonster;

        private readonly TimeOutMS mStatusCheck = new(500);
        private readonly TimeOut mDisappear = new(5);
        private readonly TimeOut mLeaveMap = new(10);

        private uint mIdAction;

        public uint GeneratorId { get; }

        public Monster(DbMonstertype type, uint identity, uint generator, uint ownerId)
        {
            mDbMonster = type;
            Identity = identity;
            GeneratorId = generator;

            OwnerIdentity = ownerId;

            Screen = new Screen(this);
        }

        #region Identity

        /// <inheritdoc />
        public override uint Identity { get; protected set; }

        public override string Name
        {
            get => mDbMonster.Name;
            set => mDbMonster.Name = value;
        }

        #endregion

        #region Initialization

        public static async Task<Monster> CreateCallPetAsync(Character caller, uint type, ushort x, ushort y)
        {
            DbMonstertype monsterType = RoleManager.GetMonstertype(type);
            if (monsterType == null)
                return null;

            uint idPet = (uint) IdentityGenerator.Pet.GetNextIdentity;
            Monster pet = new Monster(monsterType, idPet, 0, caller.Identity);
            if (!await pet.InitializeAsync(caller.MapIdentity, caller.MapX, caller.MapY))
            {
                IdentityGenerator.Pet.ReturnIdentity(idPet);
                return null;
            }

            await pet.EnterMapAsync();
            return pet;
        }

        public async Task<bool> InitializeAsync(uint idMap, ushort x, ushort y)
        {
            mIdMap = idMap;

            if ((Map = MapManager.GetMap(idMap)) == null)
                return false;

            mPosX = x;
            mPosY = y;

            Life = MaxLife;

            if (!IsCallPet())
            {
                if (mDbMonster.Action != 0 && EventManager.GetAction(mDbMonster.Action) != null)
                    mIdAction = mDbMonster.Action;
            }
            else
            {
                MsgPetInfo msg = new MsgPetInfo
                {
                    Identity = Identity,
                    LookFace = Mesh,
                    X = MapX,
                    Y = MapY,
                    Name = Name,
                    AiType = mDbMonster.AiType
                };

                Role owner = RoleManager.GetUser(OwnerIdentity);
                if (owner == null)
                    return false;
                await owner.SendAsync(msg);
            }

            if (mDbMonster.MagicType > 0)
            {
                var defaultMagic = new Magic(this);
                if (await defaultMagic.CreateAsync(mDbMonster.MagicType))
                    MagicData.Magics.TryAdd(defaultMagic.Type, defaultMagic);
            }

            foreach (DbMonsterTypeMagic dbMagic in RoleManager.GetMonsterMagics(Type))
            {
                var magic = new Magic(this);
                if (await magic.CreateAsync(dbMagic.MagicType, (ushort) dbMagic.MagicLev))
                    MagicData.Magics.TryAdd(magic.Type, magic);
            }

            return true;
        }

        #endregion

        #region Appearence

        public uint Type => mDbMonster.Id;

        public override uint Mesh
        {
            get => mDbMonster.Lookface;
            set => mDbMonster.Lookface = (ushort) value;
        }

        #endregion

        #region Battle Attributes

        public override byte Level
        {
            get => (byte) (mDbMonster?.Level ?? 0);
            set => mDbMonster.Level = value;
        }

        public override uint Life { get; set; }

        public override uint MaxLife => (uint) (mDbMonster?.Life ?? 1);

        public override int BattlePower => mDbMonster?.ExtraBattlelev ?? 0;

        public override int MinAttack => mDbMonster?.AttackMin ?? 0;

        public override int MaxAttack => mDbMonster?.AttackMax ?? 0;

        public override int ExtraDamage => mDbMonster?.ExtraDamage ?? 0;

        public override int MagicAttack => mDbMonster?.AttackMax ?? 0;

        public override int Defense => mDbMonster?.Defence ?? 0;

        public override int MagicDefense => mDbMonster?.MagicDef ?? 0;

        public override int Dodge => (int) (mDbMonster?.Dodge ?? 0);

        public override int AttackSpeed => mDbMonster?.AttackSpeed ?? 1000;

        public override int Accuracy => (int) (mDbMonster?.Dexterity ?? 0);

        public uint AttackUser => mDbMonster?.AttackUser ?? 0;

        public int ViewRange => mDbMonster?.ViewRange ?? 1;

        #endregion

        #region Drop Function

        public async Task DropItemAsync(uint type, Character owner, DropMode mode)
        {
            DbItemtype itemType = ItemManager.GetItemtype(type);
            if (itemType != null) await DropItemAsync(itemType, owner, mode);
        }

        private async Task DropItemAsync(DbItemtype itemtype, Character owner, DropMode mode)
        {
            var targetPos = new Point(MapX, MapY);
            if (Map.FindDropItemCell(4, ref targetPos))
            {
                var drop = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
                if (await drop.CreateAsync(Map, targetPos, itemtype, owner?.Identity ?? 0, 0, 0, 0, mode))
                {
                    await drop.EnterMapAsync();

                    if (drop.Info.Addition > 0 && owner?.Guide != null)
                        await owner.Guide.AwardOpportunityAsync(1);
                }
                else
                {
                    IdentityGenerator.MapItem.ReturnIdentity(drop.Identity);
                }
            }
        }

        private async Task DropEquipmentAsync(uint owner)
        {
            const int qualityPrecision = 10_000_000;

            var drop = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
            int rand = await Kernel.NextAsync(100);

            var targetPos = new Point(MapX, MapY);
            if (Map?.FindDropItemCell(4, ref targetPos) != true)
                return;

            if (rand < 60)
            {
                rand = await Kernel.NextAsync(qualityPrecision);
                var quality = 0;
                if (rand < 250)
                    quality = 9;
                else if (rand < 2500)
                    quality = 8;
                else if (rand < 12500)
                    quality = 7;
                else if (rand < 30000) quality = 6;

                MapItemInfo info = await Item.CreateItemInfoAsync(mDbMonster, quality);
                if (drop.Create(Map, targetPos, info, owner, DropMode.Common))
                    await drop.EnterMapAsync();
            }
            else
            {
                uint dropMedicine = 0;
                if (mDbMonster.DropHp != 0 && mDbMonster.DropMp != 0)
                {
                    rand = await Kernel.NextAsync(100);
                    if (rand < 60)
                        dropMedicine = mDbMonster.DropHp;
                    else
                        dropMedicine = mDbMonster.DropMp;
                }
                else if (mDbMonster.DropHp != 0)
                {
                    dropMedicine = mDbMonster.DropHp;
                }
                else
                {
                    dropMedicine = mDbMonster.DropMp;
                }

                if (dropMedicine == 0)
                    return;

                if (await drop.CreateAsync(Map, targetPos, dropMedicine, owner, 0, 0, 0, DropMode.Common))
                    await drop.EnterMapAsync();
            }
        }

        public async Task DropMoneyAsync(uint amount, uint idOwner, DropMode mode)
        {
            var targetPos = new Point(MapX, MapY);
            if (Map?.FindDropItemCell(4, ref targetPos) == true)
            {
                var drop = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity);
                if (await drop.CreateMoneyAsync(Map, targetPos, amount, idOwner, mode))
                    await drop.EnterMapAsync();
                else
                    IdentityGenerator.MapItem.ReturnIdentity(drop.Identity);
            }
        }

        #endregion

        #region Checks

        public bool IsDeleted() => mDisappear.IsActive();

        public override bool IsEvil()
        {
            return (mDbMonster.AttackUser & ATKUSER_RIGHTEOUS) == 0 || base.IsEvil();
        }

        public bool CanLeaveMap()
        {
            return mLeaveMap.IsTimeOut();
        }

        public override bool IsFarWeapon()
        {
            return mDbMonster.AttackRange > SHORTWEAPON_RANGE_LIMIT;
        }

        public bool IsPassive()
        {
            return (AttackUser & ATKUSER_PASSIVE) != 0;
        }

        public bool IsLockUser()
        {
            return (AttackUser & ATKUSER_LOCKUSER) != 0;
        }

        public bool IsRighteous()
        {
            return (AttackUser & ATKUSER_RIGHTEOUS) != 0;
        }

        public bool IsGuard()
        {
            return (AttackUser & ATKUSER_GUARD) != 0;
        }

        public bool IsPkKiller()
        {
            return (AttackUser & ATKUSER_PPKER) != 0;
        }

        public bool IsWalkEnable()
        {
            return (AttackUser & ATKUSER_FIXED) == 0;
        }

        public bool IsJumpEnable()
        {
            return (AttackUser & ATKUSER_JUMP) != 0;
        }

        public bool IsFastBack()
        {
            return (AttackUser & ATKUSER_FASTBACK) != 0;
        }

        public bool IsLockOne()
        {
            return (AttackUser & ATKUSER_LOCKONE) != 0;
        }

        public bool IsAddLife()
        {
            return (AttackUser & ATKUSER_ADDLIFE) != 0;
        }

        public bool IsEvilKiller()
        {
            return (AttackUser & ATKUSER_EVIL_KILLER) != 0;
        }

        public bool IsDormancyEnable()
        {
            return (AttackUser & ATKUSER_LOCKUSER) == 0;
        }

        public bool IsEscapeEnable()
        {
            return (AttackUser & ATKUSER_NOESCAPE) == 0;
        }

        public bool IsEquality()
        {
            return (AttackUser & ATKUSER_EQUALITY) != 0;
        }

        #endregion

        #region Battle

        public override bool SetAttackTarget(Role target)
        {
            if (target == null)
            {
                BattleSystem.ResetBattle();
                return true;
            }

            // todo check if owner user
            if (GetDistance(target) > GetAttackRange(target.SizeAddition))
                return false;

            BattleSystem.CreateBattle(target.Identity);
            return true;
        }

        public override int GetAttackRange(int sizeAdd)
        {
            return mDbMonster.AttackRange + sizeAdd;
        }

        public override bool IsAttackable(Role attacker)
        {
            if (!IsAlive)
                return false;

            return base.IsAttackable(attacker);
        }

        public override async Task BeKillAsync(Role attacker)
        {
            if (mDisappear.IsActive())
                return;

            if (attacker?.BattleSystem.IsActive() == true)
                attacker.BattleSystem.ResetBattle();

            if (attacker?.MagicData.QueryMagic != null)
                await attacker.MagicData.AbortMagicAsync(false);

            await DetachAllStatusAsync();
            await AttachStatusAsync(attacker, StatusSet.FADE, 0, int.MaxValue, 0, 0);
            await AttachStatusAsync(attacker, StatusSet.DEAD, 0, int.MaxValue, 0, 0);
            await AttachStatusAsync(attacker, StatusSet.GHOST, 0, int.MaxValue, 0, 0);

            var user = attacker as Character;
            int dieType = user?.KoCount * 65541 ?? 1;

            await BroadcastRoomMsgAsync(new MsgInteract
            {
                SenderIdentity = attacker?.Identity ?? 0,
                TargetIdentity = Identity,
                PosX = MapX,
                PosY = MapY,
                Action = MsgInteractType.Kill,
                Data = dieType
            }, false);

            mDisappear.Startup(5);
            mLeaveMap.Startup(NPC_REST_TIME);

            if (mIdAction > 0) await GameAction.ExecuteActionAsync(mIdAction, user, this, null, string.Empty);

            if (IsPkKiller() || IsGuard() || IsEvilKiller() || IsDynaNpc() || attacker == null)
                return;

            await GameAction.ExecuteActionAsync(MONSTER_DIE_ACTION, user, this, null, string.Empty);

            if (user?.Team != null)
            {
                foreach (Character member in user.Team.Members)
                    if (member.MapIdentity == user.MapIdentity
                        && member.GetDistance(user) <= Screen.VIEW_SIZE * 2)
                        await member.AddJarKillsAsync(mDbMonster.StcType);
            }
            else if (user != null)
            {
                await user.AddJarKillsAsync(mDbMonster.StcType);
            }

            int chance = await Kernel.NextAsync(100);
            var moneyAdj = 25;
            if (user != null && BattleSystem.GetNameType(user.Level, Level) == BattleSystem.NAME_GREEN)
                moneyAdj = 8;

            if (chance < moneyAdj)
            {
                var moneyMin = (int) (mDbMonster.DropMoney * 0.85f);
                var moneyMax = (int) (mDbMonster.DropMoney * 1.15f);
                var money = (uint) (moneyMin + await Kernel.NextAsync(moneyMin, moneyMax) + 1);

                int heapNum = 1 + await Kernel.NextAsync(1, 3);
                var moneyAve = (uint) (money / heapNum);

                for (var i = 0; i < heapNum; i++)
                {
                    var moneyTmp = (uint) Calculations.MulDiv((int) moneyAve, 90 + await Kernel.NextAsync(3, 21), 100);
                    await DropMoneyAsync(moneyTmp, 0, DropMode.Common);
                }
            }

            var dropNum = 0;
            int rate = await Kernel.NextAsync(0, 10000);
            chance = BattleSystem.AdjustDrop(500, attacker.Level, Level);
            if (rate < Math.Min(10000, chance))
            {
                dropNum = 1 + await Kernel.NextAsync(4, 7); // drop 5-8 items
            }
            else
            {
                chance += BattleSystem.AdjustDrop(600, attacker.Level, Level);
                if (rate < Math.Min(10000, chance))
                {
                    dropNum = 1 + await Kernel.NextAsync(2, 4); // drop 3-5 items
                }
                else
                {
                    chance += BattleSystem.AdjustDrop(750, attacker.Level, Level);
                    if (rate < Math.Min(10000, chance))
                    {
                        dropNum = 1 + await Kernel.NextAsync(1, 3); // drop 1-4 items
                    }
                    else
                    {
                        chance += BattleSystem.AdjustDrop(1500, attacker.Level, Level);
                        if (rate < Math.Min(10000, chance)) dropNum = 1; // drop 1 item
                    }
                }
            }

            for (var i = 0; i < dropNum; i++) await DropEquipmentAsync(attacker?.Identity ?? 0);
        }

        public override async Task<bool> BeAttackAsync(BattleSystem.MagicType magic, Role attacker, int nPower,
                                                       bool bReflectEnable)
        {
            if (!IsAlive)
                return false;

            await AddAttributesAsync(ClientUpdateType.Hitpoints, nPower * -1);

            if (!IsAlive)
            {
                await BeKillAsync(attacker);
                return true;
            }

            await Kernel.BroadcastWorldMsgAsync(new MsgInteract
            {
                Action = MsgInteractType.Attack,
                SenderIdentity = attacker.Identity,
                TargetIdentity = Identity
            });
            return true;
        }

        #endregion

        #region Map and Movement

        /// <summary>
        ///     The current map identity for the role.
        /// </summary>
        public override uint MapIdentity
        {
            get => mIdMap;
            set => mIdMap = value;
        }

        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public override ushort MapX
        {
            get => mPosX;
            set => mPosX = value;
        }

        /// <summary>
        ///     Current Y position of the user in the map.
        /// </summary>
        public override ushort MapY
        {
            get => mPosY;
            set => mPosY = value;
        }

        public override async Task EnterMapAsync()
        {
            Map = MapManager.GetMap(MapIdentity);
            if (Map != null)
                await Map.AddAsync(this);

            await BroadcastRoomMsgAsync(new MsgAction
            {
                Action = MsgAction<Client>.ActionType.MapEffect,
                Identity = Identity,
                X = MapX,
                Y = MapY
            }, false);

            if (IsCallPet())
            {
                await Kernel.BroadcastWorldMsgAsync(new MsgAiRoleLogin(this));
            }
        }

        public override async Task LeaveMapAsync()
        {
            IdentityGenerator.Monster.ReturnIdentity(Identity);
            if (Map != null)
            {
                await Map.RemoveAsync(Identity);
                RoleManager.RemoveRole(Identity);
            }

            Map = null;

            var msg = new MsgAiSpawnNpc
            {
                Mode = AiSpawnNpcMode.DestroyNpc
            };
            msg.List.Add(new MsgAiSpawnNpc<AiClient>.SpawnNpc
            {
                Id = Identity
            });
            await Kernel.BroadcastWorldMsgAsync(msg);
        }

        #endregion

        #region AI

        public bool IsMoveEnable()
        {
            return IsWalkEnable() || IsJumpEnable();
        }

        public bool IsCloseAttack()
        {
            return !IsBowman;
        }

        #endregion

        #region Pet

        private PetData mPetData;

        public async Task DelMonsterAsync(bool now)
        {
            if (IsDeleted())
                return;

            if (mPetData != null)
            {
                mPetData.Life = 0;
            }

            await BeKillAsync(null);

            if (now)
            {
                mDisappear.Startup(1);
                mLeaveMap.Startup(1);
                await LeaveMapAsync();
            }
            else
            {
                mDisappear.Startup(1);
                mLeaveMap.Startup(3);
            }
        }

        #endregion

        #region OnTimer

        public override async Task OnTimerAsync()
        {
            try
            {
                if (!IsAlive
                    && mDisappear.IsActive()
                    && mDisappear.IsTimeOut())
                    QueueAction(async () => await AttachStatusAsync(this, StatusSet.INVISIBLE, 0, int.MaxValue, 0, 0));
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Monster::OnTimerAsync() => {Identity}:{Name} Set ghost: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mLeaveMap.IsActive())
                {
                    if (CanLeaveMap())
                        QueueAction(LeaveMapAsync);
                    return;
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Monster::OnTimerAsync() => {Identity}:{Name} LeaveMapAsync: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mStatusCheck.ToNextTime())
                    foreach (IStatus stts in StatusSet.Status.Values)
                    {
                        QueueAction(async () => await stts.OnTimerAsync());

                        if (!stts.IsValid && stts.Identity != StatusSet.GHOST && stts.Identity != StatusSet.DEAD)
                            await StatusSet.DelObjAsync(stts.Identity);
                    }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Monster::OnTimerAsync() => {Identity}:{Name} Status: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }


            try
            {
                if (BattleSystem != null
                    && BattleSystem.IsActive()
                    && BattleSystem.NextAttack(AttackSpeed))
                    QueueAction(BattleSystem.ProcessAttackAsync);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Monster::OnTimerAsync() => {Identity}:{Name} BattleSystem.ProcessAttackAsync(): {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (MagicData.State != MagicData.MagicState.None)
                    QueueAction(MagicData.OnTimerAsync);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Monster::OnTimerAsync() => {Identity}:{Name} MagicData.OnTimerAsync(): {ex.Message}");
                await Log.WriteLogAsync(ex);
            }
        }

        #endregion

        #region Socket

        public override Task SendSpawnToAsync(Character player)
        {
            return player.SendAsync(new MsgPlayer(this));
        }

        public override Task SendSpawnToAsync(Character player, int x, int y)
        {
            return player.SendAsync(new MsgPlayer(this, (ushort) x, (ushort) y));
        }

        #endregion

        #region Constants

        public const int ATKUSER_LEAVEONLY = 0,        // Ö»»áÌÓÅÜ
                         ATKUSER_PASSIVE = 0x01,       // ±»¶¯¹¥»÷
                         ATKUSER_ACTIVE = 0x02,        // Ö÷¶¯¹¥»÷
                         ATKUSER_RIGHTEOUS = 0x04,     // ÕýÒåµÄ(ÎÀ±ø»òÍæ¼ÒÕÙ»½ºÍ¿ØÖÆµÄ¹ÖÎï)
                         ATKUSER_GUARD = 0x08,         // ÎÀ±ø(ÎÞÊÂ»ØÔ­Î»ÖÃ)
                         ATKUSER_PPKER = 0x10,         // ×·É±ºÚÃû 
                         ATKUSER_JUMP = 0x20,          // »áÌø
                         ATKUSER_FIXED = 0x40,         // ²»»á¶¯µÄ
                         ATKUSER_FASTBACK = 0x0080,    // ËÙ¹é
                         ATKUSER_LOCKUSER = 0x0100,    // Ëø¶¨¹¥»÷Ö¸¶¨Íæ¼Ò£¬Íæ¼ÒÀë¿ª×Ô¶¯ÏûÊ§ 
                         ATKUSER_LOCKONE = 0x0200,     // Ëø¶¨¹¥»÷Ê×ÏÈ¹¥»÷×Ô¼ºµÄÍæ¼Ò
                         ATKUSER_ADDLIFE = 0x0400,     // ×Ô¶¯¼ÓÑª
                         ATKUSER_EVIL_KILLER = 0x0800, // °×ÃûÉ±ÊÖ
                         ATKUSER_WING = 0x1000,        // ·ÉÐÐ×´Ì¬
                         ATKUSER_NEUTRAL = 0x2000,     // ÖÐÁ¢
                         ATKUSER_ROAR = 0x4000,        // ³öÉúÊ±È«µØÍ¼Å­ºð
                         ATKUSER_NOESCAPE = 0x8000,    // ²»»áÌÓÅÜ
                         ATKUSER_EQUALITY = 0x10000;   // ²»ÃêÊÓ

        public const int SHORTWEAPON_RANGE_LIMIT = 2; // ½üÉíÎäÆ÷µÄ×î´ó¹¥»÷·¶Î§(ÒÔ´Ë·¶Î§ÊÇ·ñ¹­¼ýÊÖ)
        public const int NPC_REST_TIME = 7;

        private enum AiStage
        {
            /// <summary>
            ///     Monster wont do nothing, just heal. Activated if on active block but haven't triggered any other action.
            /// </summary>
            Dormancy,

            /// <summary>
            ///     Monster is doing absolutely nothing.
            /// </summary>
            Idle,

            /// <summary>
            ///     When monster is low life and want to run from the attacker.
            /// </summary>
            Escape,

            /// <summary>
            ///     Monster movement.
            /// </summary>
            Forward,

            /// <summary>
            ///     Monster is ready for attack.
            /// </summary>
            Attack,
            Last
        }

        #endregion
    }

    public class PetData
    {
        public uint OwnerIdentity;
        public uint OwnerType;
        public uint Generator;
        public uint Type;
        public string Name;
        public uint Life;
        public uint Mana;
        public uint MapIdentity;
        public ushort MapX;
        public ushort MapY;
        public object Data;
    }
}