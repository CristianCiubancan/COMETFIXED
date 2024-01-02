using System;
using System.Drawing;
using System.Threading.Tasks;
using Comet.AI.Packets;
using Comet.AI.World.Managers;
using Comet.AI.World.Maps;
using Comet.Core.World.Maps;
using Comet.Game.Packets;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.AI.States
{
    public abstract class Role
    {
        protected uint m_idMap;

        protected uint m_maxLife = 0,
                       m_maxMana = 0;

        protected ushort m_posX,
                         m_posY;

        protected Role()
        {
            StatusSet = new StatusSet(this);
        }

        #region Appearence

        public virtual uint Mesh { get; set; }

        #endregion

        #region Level

        public virtual byte Level { get; set; }

        #endregion

        #region Processor Queue

        public void QueueAction(Func<Task> task)
        {
            if (Map == null)
                return;

            Kernel.Services.Processor.Queue(Map.Partition, task);
        }

        #endregion

        #region Timers

        public virtual Task OnTimerAsync()
        {
            return Task.CompletedTask;
        }

        #endregion

        #region Identity

        /// <summary>
        ///     The identity of the role in the world. This will be unique for ANY role in the world.
        /// </summary>
        public virtual uint Identity { get; protected set; }

        /// <summary>
        ///     The name of the role. May be empty or null for NPCs and Dynamic NPCs.
        /// </summary>
        public virtual string Name { get; set; }

        #endregion

        #region Life and Mana

        public virtual bool IsAlive => Life > 0;
        public virtual uint Life { get; set; }
        public virtual uint MaxLife => m_maxLife;
        public virtual uint Mana { get; set; }
        public virtual uint MaxMana => m_maxMana;

        #endregion

        #region Map and Position

        public virtual GameMap Map { get; protected set; }

        /// <summary>
        ///     The current map identity for the role.
        /// </summary>
        public virtual uint MapIdentity
        {
            get => m_idMap;
            set => m_idMap = value;
        }

        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public virtual ushort MapX
        {
            get => m_posX;
            set => m_posX = value;
        }

        /// <summary>
        ///     Current Y position of the user in the map.
        /// </summary>
        public virtual ushort MapY
        {
            get => m_posY;
            set => m_posY = value;
        }

        public virtual int GetDistance(Role role)
        {
            if (role.MapIdentity != MapIdentity) return int.MaxValue;
            return GetDistance(role.MapX, role.MapY);
        }

        public virtual int GetDistance(int x, int y)
        {
            return Calculations.GetDistance(MapX, MapY, x, y);
        }

        /// <summary>
        /// </summary>
        public virtual Task EnterMapAsync(bool sync = true)
        {
            Map = MapManager.GetMap(MapIdentity);
            if (Map != null)
                return Map.AddAsync(this);
            return Task.CompletedTask;
        }

        /// <summary>
        /// </summary>
        public virtual async Task LeaveMapAsync(bool sync = true)
        {
            if (Map != null)
            {
                await Map.RemoveAsync(Identity);
                RoleManager.RemoveRole(Identity);
            }

            Map = null;
        }

        #endregion

        #region Movement

        public async Task<bool> JumpPosAsync(int x, int y, bool sync = true)
        {
            if (x == MapX && y == MapY)
                return false;

            if (Map == null || !Map.IsValidPoint(x, y))
                return false;

            Character user = null;
            if (IsPlayer())
            {
                user = (Character) this;
                user.ClearProtection();
            }

            Map.EnterBlock(this, x, y, MapX, MapY);

            Direction = (FacingDirection) Calculations.GetDirectionSector(MapX, MapY, x, y);

            if (sync)
                await Kernel.SendAsync(new MsgAction
                {
                    CommandX = (ushort) x,
                    CommandY = (ushort) y,
                    X = m_posX,
                    Y = m_posY,
                    Identity = Identity,
                    Action = MsgAction<Server>.ActionType.MapJump,
                    Direction = (ushort) Direction,
                    Timestamp = (uint) Environment.TickCount
                });

            m_posX = (ushort) x;
            m_posY = (ushort) y;

            return true;
        }

        public async Task<bool> MoveTowardAsync(int direction, int mode, bool sync = true)
        {
            ushort newX = 0, newY = 0;

            if (mode == (int) RoleMoveMode.Track)
            {
                direction %= 24;
                newX = (ushort) (MapX + GameMapData.RideXCoords[direction]);
                newY = (ushort) (MapY + GameMapData.RideYCoords[direction]);
            }
            else
            {
                direction %= 8;
                newX = (ushort) (MapX + GameMapData.WalkXCoords[direction]);
                newY = (ushort) (MapY + GameMapData.WalkYCoords[direction]);

                bool isRunning = mode is >= (int) RoleMoveMode.RunDir0 and <= (int) RoleMoveMode.RunDir7;
                if (isRunning && IsAlive)
                {
                    newX += (ushort) GameMapData.WalkXCoords[direction];
                    newY += (ushort) GameMapData.WalkYCoords[direction];
                }
            }

            Character user = null;
            if (IsPlayer())
            {
                user = (Character)this;
                user.ClearProtection();
            }

            Map.EnterBlock(this, newX, newY, MapX, MapY);

            Direction = (FacingDirection) direction;

            if (sync)
                await Kernel.SendAsync(new MsgWalk
                {
                    Direction = (byte) direction,
                    Identity = Identity,
                    Mode = (byte) mode
                });

            m_posX = newX;
            m_posY = newY;

            return true;
        }

        public async Task<bool> JumpBlockAsync(int x, int y, FacingDirection dir)
        {
            int steps = GetDistance(x, y);

            if (steps <= 0)
                return false;

            for (var i = 0; i < steps; i++)
            {
                var pos = new Point(MapX + (x - MapX) * i / steps, MapY + (y - MapY) * i / steps);
                if (Map.IsStandEnable(pos.X, pos.Y))
                {
                    await JumpPosAsync(pos.X, pos.Y);
                    return true;
                }
            }

            if (Map.IsStandEnable(x, y))
            {
                await JumpPosAsync(x, y);
                return true;
            }

            return false;
        }

        public async Task<bool> FarJumpAsync(int x, int y, FacingDirection dir)
        {
            int steps = GetDistance(x, y);

            if (steps <= 0)
                return false;

            if (Map.IsStandEnable(x, y))
            {
                await JumpPosAsync(x, y);
                return true;
            }

            for (var i = 0; i < steps; i++)
            {
                var pos = new Point(MapX + (x - MapX) * i / steps, MapY + (y - MapY) * i / steps);
                if (Map.IsStandEnable(pos.X, pos.Y))
                {
                    await JumpPosAsync(pos.X, pos.Y);
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region Action and Direction

        public virtual FacingDirection Direction { get; protected set; }

        public virtual EntityAction Action { get; protected set; }

        public async Task SetDirectionAsync(FacingDirection direction, bool sync = true)
        {
            Direction = direction;
            if (sync)
                await Kernel.SendAsync(new MsgAction
                {
                    Identity = Identity,
                    Action = MsgAction<Server>.ActionType.CharacterDirection,
                    Direction = (ushort) direction,
                    ArgumentX = MapX,
                    ArgumentY = MapY
                });
        }

        public async Task SetActionAsync(EntityAction action, bool sync = true)
        {
            if (action != EntityAction.Cool && IsWing)
                return;

            Action = action;
            if (sync)
                await Kernel.SendAsync(new MsgAction
                {
                    Identity = Identity,
                    Action = MsgAction<Server>.ActionType.CharacterEmote,
                    Command = (ushort) action,
                    ArgumentX = MapX,
                    ArgumentY = MapY,
                    Direction = (ushort) Direction
                });
        }

        #endregion

        #region Role Type

        public static bool IsPlayer(uint id)
        {
            return id is >= PLAYER_ID_FIRST and < PLAYER_ID_LAST;
        }

        public bool IsPlayer()
        {
            return IsPlayer(Identity);
        }

        public bool IsMonster()
        {
            return Identity is >= MONSTERID_FIRST and < MONSTERID_LAST;
        }

        public bool IsNpc()
        {
            return Identity is >= SYSNPCID_FIRST and < SYSNPCID_LAST;
        }

        public bool IsDynaNpc()
        {
            return Identity is >= DYNANPCID_FIRST and < DYNANPCID_LAST;
        }

        public static bool IsCallPet(uint id)
        {
            return id is >= CALLPETID_FIRST and < CALLPETID_LAST;
        }

        public bool IsCallPet()
        {
            return IsCallPet(Identity);
        }

        public bool IsTrap()
        {
            return Identity is >= TRAPID_FIRST and < TRAPID_LAST;
        }

        public bool IsMapItem()
        {
            return Identity is >= MAPITEM_FIRST and < MAPITEM_LAST;
        }

        public bool IsFurniture()
        {
            return Identity is >= SCENE_NPC_MIN and < SCENE_NPC_MAX;
        }

        #endregion

        #region Battle Attributes

        public virtual int ViewRange { get; } = 18;
        public virtual int BattlePower => 1;

        public virtual int MinAttack { get; } = 1;
        public virtual int MaxAttack { get; } = 1;
        public virtual int MagicAttack { get; } = 1;
        public virtual int Defense { get; } = 0;
        public virtual int MagicDefense { get; } = 0;
        public virtual int MagicDefenseBonus { get; } = 0;
        public virtual int Dodge { get; } = 0;
        public virtual int AttackSpeed { get; } = 1000;
        public virtual int Accuracy { get; } = 1;
        public virtual int Defense2 => Calculations.DEFAULT_DEFENCE2;
        public virtual int Blessing { get; } = 0;

        public virtual int AddFinalAttack { get; } = 0;
        public virtual int AddFinalMAttack { get; } = 0;
        public virtual int AddFinalDefense { get; } = 0;
        public virtual int AddFinalMDefense { get; } = 0;

        public virtual int ExtraDamage { get; } = 0;

        #endregion

        #region Battle Processing

        public int SizeAddition => 1;

        public virtual Task<bool> CheckCrimeAsync(Role target)
        {
            return Task.FromResult(false);
        }

        public virtual int AdjustWeaponDamage(int damage)
        {
            return Calculations.MulDiv(damage, Defense2, Calculations.DEFAULT_DEFENCE2);
        }

        public int AdjustMagicDamage(int damage)
        {
            return Calculations.MulDiv(damage, Defense2, Calculations.DEFAULT_DEFENCE2);
        }

        public virtual Task<bool> BeAttackAsync(Role attacker)
        {
            return Task.FromResult(false);
        }

        public virtual int GetAttackRange(int sizeAdd)
        {
            return sizeAdd + 1;
        }

        public virtual bool IsAttackable(Role attacker)
        {
            return true;
        }

        public virtual bool IsFarWeapon()
        {
            return false;
        }

        #endregion

        #region Status

        protected const int maxStatus = 64;

        public ulong StatusFlag { get; set; }

        public StatusSet StatusSet { get; }

        public virtual async Task<bool> DetachWellStatusAsync()
        {
            for (var i = 1; i < maxStatus; i++)
                if (StatusSet[i] != null)
                    if (IsWellStatus(i))
                        await DetachStatusAsync(i);
            return true;
        }

        public virtual async Task<bool> DetachBadlyStatusAsync()
        {
            for (var i = 1; i < maxStatus; i++)
                if (StatusSet[i] != null)
                    if (IsBadlyStatus(i))
                        await DetachStatusAsync(i);
            return true;
        }

        public virtual async Task<bool> DetachAllStatusAsync()
        {
            await DetachBadlyStatusAsync();
            await DetachWellStatusAsync();
            return true;
        }

        public virtual bool IsWellStatus(int stts)
        {
            switch (stts)
            {
                case StatusSet.RIDING:
                case StatusSet.FULL_INVIS:
                case StatusSet.LUCKY_DIFFUSE:
                case StatusSet.STIG:
                case StatusSet.SHIELD:
                case StatusSet.STAR_OF_ACCURACY:
                case StatusSet.START_XP:
                case StatusSet.INVISIBLE:
                case StatusSet.SUPERMAN:
                case StatusSet.CYCLONE:
                case StatusSet.PARTIALLY_INVISIBLE:
                case StatusSet.LUCKY_ABSORB:
                case StatusSet.VORTEX:
                case StatusSet.FLY:
                case StatusSet.FATAL_STRIKE:
                case StatusSet.AZURE_SHIELD:
                case StatusSet.SUPER_SHIELD_HALO:
                case StatusSet.CARYING_FLAG:
                case StatusSet.EARTH_AURA:
                case StatusSet.FEND_AURA:
                case StatusSet.FIRE_AURA:
                case StatusSet.METAL_AURA:
                case StatusSet.TYRANT_AURA:
                case StatusSet.WATER_AURA:
                case StatusSet.WOOD_AURA:
                case StatusSet.OBLIVION:
                case StatusSet.CTF_FLAG:
                    return true;
            }

            return false;
        }

        public virtual bool IsBadlyStatus(int stts)
        {
            switch (stts)
            {
                case StatusSet.POISONED:
                case StatusSet.CONFUSED:
                case StatusSet.ICE_BLOCK:
                case StatusSet.HUGE_DAZED:
                case StatusSet.DAZED:
                case StatusSet.SHACKLED:
                case StatusSet.POISON_STAR:
                case StatusSet.TOXIC_FOG:
                    return true;
            }

            return false;
        }

        public virtual async Task<bool> AttachStatusAsync(Role pSender, int nStatus, int nPower, int nSecs, int nTimes,
                                                          byte pLevel, bool save = false)
        {
            if (Map == null)
                return false;

            IStatus pStatus = QueryStatus(nStatus);
            if (pStatus != null)
            {
                var bChangeData = false;
                if (pStatus.Power == nPower)
                {
                    bChangeData = true;
                }
                else
                {
                    int nMinPower = Math.Min(nPower, pStatus.Power);
                    int nMaxPower = Math.Max(nPower, pStatus.Power);

                    if (nPower <= 30000)
                    {
                        bChangeData = true;
                    }
                    else
                    {
                        if (nMinPower >= 30100 || nMinPower > 0 && nMaxPower < 30000)
                        {
                            if (nPower > pStatus.Power)
                                bChangeData = true;
                        }
                        else if (nMaxPower < 0 || nMinPower > 30000 && nMaxPower < 30100)
                        {
                            if (nPower < pStatus.Power)
                                bChangeData = true;
                        }
                    }
                }

                if (bChangeData) await pStatus.ChangeDataAsync(nPower, nSecs, nTimes, pSender?.Identity ?? 0);
                return true;
            }

            if (nTimes > 1)
            {
                var pNewStatus = new StatusMore();
                if (await pNewStatus.CreateAsync(this, nStatus, nPower, nSecs, nTimes, pSender?.Identity ?? 0, pLevel))
                {
                    await StatusSet.AddObjAsync(pNewStatus);
                    return true;
                }
            }
            else
            {
                var pNewStatus = new StatusOnce();
                if (await pNewStatus.CreateAsync(this, nStatus, nPower, nSecs, 0, pSender?.Identity ?? 0, pLevel))
                {
                    await StatusSet.AddObjAsync(pNewStatus);
                    return true;
                }
            }

            return false;
        }

        public virtual async Task<bool> DetachStatusAsync(int nType)
        {
            return await StatusSet.DelObjAsync(nType);
        }

        public virtual async Task<bool> DetachStatusAsync(ulong nType, bool b64)
        {
            return await StatusSet.DelObjAsync(StatusSet.InvertFlag(nType, b64));
        }

        public virtual bool IsOnXpSkill()
        {
            return false;
        }

        public virtual IStatus QueryStatus(int nType)
        {
            return StatusSet?.GetObjByIndex(nType);
        }

        public bool IsGhost()
        {
            return QueryStatus(StatusSet.GHOST) != null;
        }

        public async Task SetCrimeStatusAsync(int nSecs)
        {
            await AttachStatusAsync(this, StatusSet.BLUE_NAME, 0, nSecs, 0, 0);
        }

        public virtual bool IsWing => QueryStatus(StatusSet.FLY) != null;

        public virtual bool IsBowman => false;

        public virtual bool IsShieldUser => false;

        public virtual bool IsEvil()
        {
            return QueryStatus(StatusSet.BLUE_NAME) != null || QueryStatus(StatusSet.BLACK_NAME) != null;
        }

        public bool IsVirtuous()
        {
            return (StatusFlag & KEEP_EFFECT_NOT_VIRTUOUS) == 0;
        }

        public bool IsCrime()
        {
            return QueryStatus(StatusSet.BLUE_NAME) != null;
        }

        public bool IsPker()
        {
            return QueryStatus(StatusSet.BLACK_NAME) != null;
        }

        #endregion

        #region Synchronization

        public virtual async Task<bool> AddAttributesAsync(ClientUpdateType type, long value)
        {
            long currAttr = 0;
            switch (type)
            {
                case ClientUpdateType.Hitpoints:
                    currAttr = Life = (uint) Math.Min(MaxLife, Math.Max(Life + value, 0));
                    break;

                case ClientUpdateType.Mana:
                    currAttr = Mana = (uint) Math.Min(MaxMana, Math.Max(Mana + value, 0));
                    break;

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"Role::AddAttributes {type} not handled");
                    return false;
            }

            return true;
        }

        public virtual async Task<bool> SetAttributesAsync(ClientUpdateType type, ulong value)
        {
            var screen = false;
            switch (type)
            {
                case ClientUpdateType.Hitpoints:
                    value = Life = (uint) Math.Max(0, Math.Min(MaxLife, value));
                    break;

                case ClientUpdateType.Mana:
                    value = Mana = (uint) Math.Max(0, Math.Min(MaxMana, value));
                    break;

                case ClientUpdateType.StatusFlag:
                    StatusFlag = value;
                    screen = true;
                    break;

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"Role::SetAttributes {type} not handled");
                    return false;
            }

            return true;
        }

        #endregion

        #region Socket

        public virtual async Task SendAsync(IPacket msg)
        {
            await Log.WriteLogAsync(LogLevel.Warning, $"{GetType().Name} - {Identity} has no SendAsync handler");
        }

        public virtual async Task SendSpawnToAsync(Character player)
        {
            await Log.WriteLogAsync(LogLevel.Warning, $"{GetType().Name} - {Identity} has no SendSpawnToAsync handler");
        }

        #endregion

        #region Comparison

        public override bool Equals(object obj)
        {
            return obj is Role role && role.Identity == Identity;
        }

        public override int GetHashCode()
        {
            return Identity.GetHashCode();
        }

        #endregion

        #region Constants

        public static ulong KEEP_EFFECT_NOT_VIRTUOUS => StatusSet.GetFlag(StatusSet.BLUE_NAME) |
                                                        StatusSet.GetFlag(StatusSet.RED_NAME) |
                                                        StatusSet.GetFlag(StatusSet.BLACK_NAME);

        public const int SCENEID_FIRST = 1;
        public const int SYSNPCID_FIRST = 1;
        public const int SYSNPCID_LAST = 99999;
        public const int DYNANPCID_FIRST = 100000;
        public const int DYNANPCID_LAST = 199999;
        public const int SCENE_NPC_MIN = 200000;
        public const int SCENE_NPC_MAX = 299999;
        public const int SCENEID_LAST = 299999;

        public const int NPCSERVERID_FIRST = 400001;
        public const int MONSTERID_FIRST = 400001;
        public const int MONSTERID_LAST = 499999;
        public const int PETID_FIRST = 500001;
        public const int PETID_LAST = 599999;
        public const int NPCSERVERID_LAST = 699999;

        public const int CALLPETID_FIRST = 700001;
        public const int CALLPETID_LAST = 799999;

        public const int MAPITEM_FIRST = 800001;
        public const int MAPITEM_LAST = 899999;

        public const int TRAPID_FIRST = 900001;
        public const int MAGICTRAPID_FIRST = 900001;
        public const int MAGICTRAPID_LAST = 989999;
        public const int SYSTRAPID_FIRST = 990001;
        public const int SYSTRAPID_LAST = 999999;
        public const int TRAPID_LAST = 999999;

        public const int DYNAMAP_FIRST = 1000000;

        public const int PLAYER_ID_FIRST = 1000000;
        public const int PLAYER_ID_LAST = 1999999999;

        public const int NPCDIEDELAY_SECS = 10;

        #endregion
    }

    public enum FacingDirection : byte
    {
        Begin = SouthEast,
        SouthWest = 0,
        West = 1,
        NorthWest = 2,
        North = 3,
        NorthEast = 4,
        East = 5,
        SouthEast = 6,
        South = 7,
        End = South,
        Invalid = End + 1
    }

    public enum EntityAction : ushort
    {
        Dance1 = 1,
        Dance2 = 2,
        Dance3 = 3,
        Dance4 = 4,
        Dance5 = 5,
        Dance6 = 6,
        Dance7 = 7,
        Dance8 = 8,
        Stand = 100,
        Happy = 150,
        Angry = 160,
        Sad = 170,
        Wave = 190,
        Bow = 200,
        Kneel = 210,
        Cool = 230,
        Sit = 250,
        Lie = 270
    }
}