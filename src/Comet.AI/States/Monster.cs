using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.AI.Packets;
using Comet.AI.World;
using Comet.AI.World.Managers;
using Comet.Core;
using Comet.Core.World.Maps;
using Comet.Database.Entities;
using Comet.Network.Packets.Ai;
using Comet.Network.Packets.Game;
using Comet.Shared;
using static Comet.Network.Packets.Game.MsgWalk<Comet.AI.States.Server>;

namespace Comet.AI.States
{
    public sealed class Monster : Role
    {
        private readonly TimeOut mAction = new();
        private readonly DbMonstertype mDbMonster;
        private readonly Generator mGenerator;
        private readonly TimeOut mLeaveMap = new(10);

        private readonly List<MonsterMagic> mMagics = new();
        private readonly TimeOutMS mMove = new();

        private AiStage mStage = AiStage.Idle;

        public Monster(DbMonstertype type, uint identity, Generator generator)
        {
            mDbMonster = type;
            mGenerator = generator;
            Identity = identity;
            m_idMap = generator.MapIdentity;

            mAction.Startup(AttackSpeed * 3 / 1000);
        }

        #region Identity

        public override string Name
        {
            get => mDbMonster.Name;
            set => mDbMonster.Name = value;
        }

        #endregion

        #region Initialization

        public async Task<bool> InitializeAsync(uint idMap, ushort x, ushort y)
        {
            m_idMap = idMap;

            if ((Map = MapManager.GetMap(idMap)) == null)
                return false;

            m_posX = x;
            m_posY = y;

            Life = MaxLife;

            foreach (DbMonsterTypeMagic dbm in RoleManager.GetMonsterMagics(Type)) mMagics.Add(new MonsterMagic(dbm));

            if (mDbMonster.MagicType != 0)
                mMagics.Add(new MonsterMagic(new DbMonsterTypeMagic
                {
                    MagicType = mDbMonster.MagicType,
                    MagicLev = 0,
                    ColdTime = (uint) Math.Max(500, mDbMonster.AttackSpeed)
                }));
            return true;
        }

        #endregion

        #region OnTimer

        public override async Task OnTimerAsync()
        {
            if (Map == null)
                return;

            if (!IsAlive)
                return;

            for (var i = 0; i < 5; i++)
                switch (mStage)
                {
                    case AiStage.Idle:
                        if (await OnIdleAsync())
                            return;
                        break;
                    case AiStage.Forward:
                        if (await OnForwardAsync())
                            return;
                        break;
                    case AiStage.Attack:
                        if (await OnAttackAsync())
                            return;
                        break;
                    case AiStage.Escape:
                        if (await OnEscapeAsync())
                            return;
                        break;
                }
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

        #region Checks

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

        #region Map and Movement

        /// <summary>
        ///     The current map identity for the role.
        /// </summary>
        public override uint MapIdentity
        {
            get => m_idMap;
            set => m_idMap = value;
        }

        /// <summary>
        ///     Current X position of the user in the map.
        /// </summary>
        public override ushort MapX
        {
            get => m_posX;
            set => m_posX = value;
        }

        /// <summary>
        ///     Current Y position of the user in the map.
        /// </summary>
        public override ushort MapY
        {
            get => m_posY;
            set => m_posY = value;
        }

        public override async Task EnterMapAsync(bool sync = true)
        {
            Map = MapManager.GetMap(MapIdentity);
            if (Map != null && await Map.AddAsync(this))
            {
                if (sync)
                {
                    var msg = new MsgAiSpawnNpc();
                    msg.Mode = AiSpawnNpcMode.Spawn;
                    msg.List.Add(new MsgAiSpawnNpc<Server>.SpawnNpc
                    {
                        Id = Identity,
                        GeneratorId = mGenerator.Identity,
                        MapId = MapIdentity,
                        X = MapX,
                        Y = MapY,
                        MonsterType = Type,
                        OwnerId = 0
                    });
                    await Kernel.SendAsync(msg);
                }
            }
        }

        public override async Task LeaveMapAsync(bool sync = true)
        {
            mGenerator.Remove(Identity);
            IdentityGenerator.Monster.ReturnIdentity(Identity);

            if (sync)
            {
                var msg = new MsgAiSpawnNpc();
                msg.Mode = AiSpawnNpcMode.DestroyNpc;
                msg.List.Add(new MsgAiSpawnNpc<Server>.SpawnNpc
                {
                    Id = Identity
                });
                await Kernel.SendAsync(msg);
            }

            if (Map != null)
            {
                await Map.RemoveAsync(Identity);
                RoleManager.RemoveRole(Identity);
            }

            Map = null;
        }

        #endregion

        #region Battle

        public override int GetAttackRange(int sizeAdd)
        {
            return mDbMonster.AttackRange + sizeAdd;
        }

        public override bool IsAlive => Life > 0 && QueryStatus(StatusSet.GHOST) == null;

        public override async Task<bool> BeAttackAsync(Role attacker)
        {
            if (!IsAlive)
                return false;

            mIdAtkMe = attacker.Identity;

            if (!IsMoveEnable())
                return false;

            if (IsEscapeEnable())
            {
                if (!IsPassive())
                {
                    mActTarget = mMoveTarget = mIdAtkMe;
                    await ChangeModeAsync(AiStage.Escape);
                    return true;
                }

                if (Life < mDbMonster.EscapeLife)
                {
                    await ChangeModeAsync(AiStage.Escape);
                    return true;
                }
            }

            int distance = GetDistance(attacker);
            if (IsFarWeapon() && !attacker.IsFarWeapon() && distance <= SHORTWEAPON_RANGE_LIMIT)
            {
                mActTarget = mMoveTarget = mIdAtkMe;

                FindPath(GetAttackRange(attacker.SizeAddition) - distance);
                await ChangeModeAsync(AiStage.Forward);
                return true;
            }

            if (!IsAttackable(attacker))
                return false;

            if (await Kernel.NextAsync(100) < 80)
            {
                if (mStage == AiStage.Escape && await Kernel.NextAsync(100) < 80)
                    return false;

                mActTarget = mMoveTarget = mIdAtkMe;
                if (await Kernel.NextAsync(100) < 80)
                {
                    await ChangeModeAsync(AiStage.Forward);
                    FindPath();

                    if (mNextDir != FacingDirection.Invalid)
                        return true;
                }

                if (IsEscapeEnable())
                {
                    await ChangeModeAsync(AiStage.Escape);
                    FindPath();
                }
            }

            return true;
        }

        public override bool IsAttackable(Role target)
        {
            if (target.IsWing && !IsWing && IsCloseAttack())
                return false;
            return true;
        }

        #endregion

        #region AI

        private uint mMoveTarget;
        private uint mActTarget;
        private uint mIdAtkMe;

        public bool IsMoveEnable()
        {
            return IsWalkEnable() || IsJumpEnable();
        }

        public bool IsCloseAttack()
        {
            return !IsBowman;
        }

        private bool CheckTarget()
        {
            Role target = RoleManager.GetRole(mActTarget);
            if (target == null || !target.IsAlive)
            {
                mMoveTarget = 0;
                mActTarget = 0;
                return false;
            }

            if (target.IsWing && !IsWing && !IsCloseAttack())
                return false;

            int nDistance = ViewRange;
            int nAtkDistance = Calculations.CutOverflow(nDistance, GetAttackRange(target.SizeAddition));
            int nDist = GetDistance(target.MapX, target.MapY);

            if (!(nDist <= nDistance) || nDist <= nAtkDistance && GetAttackRange(target.SizeAddition) > 1)
            {
                mActTarget = 0;
                return false;
            }

            return true;
        }

        private async Task<bool> FindNewTargetAsync()
        {
            if (IsLockUser() || IsLockOne())
            {
                if (CheckTarget())
                {
                    if (IsLockOne())
                        return true;
                }
                else
                {
                    if (IsLockUser())
                        return false;
                }
            }

            uint idOldTarget = mActTarget;
            int distance = ViewRange;
            List<Role> roles = Map.Query9BlocksByPos(MapX, MapY);
            foreach (Role role in roles.Distinct())
            {
                if (role.Identity == Identity)
                    continue;

                if (!role.IsAlive)
                    continue;

                if (role.QueryStatus(StatusSet.INVISIBLE) != null)
                    continue;

                if (role is Character targetUser)
                {
                    bool pkKill = IsPkKiller() && targetUser.IsPker();
                    bool evilKill = IsEvilKiller() && !targetUser.IsVirtuous();
                    bool evilMob = IsEvil() && !(IsPkKiller() || IsEvilKiller());

                    if (IsGuard() && targetUser.IsCrime()
                        || pkKill
                        || evilKill
                        || evilMob)
                    {
                        if (targetUser.IsWing && !IsWing && !IsBowman)
                            continue;

                        if (!targetUser.IsAttackable(this))
                            continue;

                        int nDist = GetDistance(targetUser.MapX, targetUser.MapY);
                        if (nDist <= distance)
                        {
                            distance = nDist;
                            mMoveTarget = mActTarget = targetUser.Identity;

                            if (pkKill || evilKill)
                                break;
                        }
                    }
                }
                else if (role is Monster monster)
                {
                    if (IsEvil() && monster.IsRighteous()
                        || IsRighteous() && monster.IsEvil())
                    {
                        if (monster.IsWing && !IsWing) continue;

                        int nDist = GetDistance(monster.MapX, monster.MapY);
                        if (nDist < distance)
                        {
                            distance = nDist;
                            mMoveTarget = mActTarget = monster.Identity;
                        }
                    }
                }
            }

            if (mActTarget != 0)
            {
                Role role = RoleManager.GetRole(mActTarget);
                if (role is Character targetUser && targetUser.Identity != idOldTarget)
                {
                    if (IsGuard() && targetUser.IsCrime())
                        await Kernel.SendAsync(new MsgTalk(Identity, TalkChannel.Talk, Color.White, targetUser.Name,
                                                           Name,
                                                           Language.StrGuardYouPay));
                    else if (IsPkKiller() && targetUser.IsPker() && mStage == AiStage.Idle)
                        await Kernel.SendAsync(new MsgTalk(Identity, TalkChannel.Talk, Color.White, targetUser.Name,
                                                           Name,
                                                           Language.StrGuardYouPay));
                }

                FindPath();

                return mMoveTarget != 0;
            }

            return true;
        }

        private async Task<bool> OnIdleAsync()
        {
            if (!(IsGuard() || IsPkKiller() || IsEvilKiller()))
                if (!(IsFastBack() && !mGenerator.IsInRegion(MapX, MapY)))
                    if (!mAction.ToNextTime())
                        return true;

            if (await FindNewTargetAsync())
            {
                Role target = RoleManager.GetRole(mMoveTarget);
                if (target != null)
                {
                    int distance = GetDistance(target);
                    if (distance <= GetAttackRange(target.SizeAddition))
                    {
                        await ChangeModeAsync(AiStage.Attack);
                        return false;
                    }

                    if (IsMoveEnable())
                    {
                        if (mNextDir == FacingDirection.Invalid)
                        {
                            if (!IsEscapeEnable() || await Kernel.NextAsync(100) < 80)
                                return true;

                            await ChangeModeAsync(AiStage.Escape);
                            return false;
                        }

                        await ChangeModeAsync(AiStage.Forward);
                        return false;
                    }
                }
            }

            if (IsGuard() || IsPkKiller() || IsEvilKiller())
                if (!mAction.ToNextTime())
                    return true;

            if (!IsMoveEnable())
                return true;

            if (mGenerator.IsInRegion(MapX, MapY))
            {
                if (IsGuard() || IsPkKiller() || IsEvilKiller())
                {
                    if (await Kernel.NextAsync(100) < 5 && mGenerator.GetWidth() > 1 || mGenerator.GetHeight() > 1)
                    {
                        int x = MapX + await Kernel.NextAsync(mGenerator.GetWidth());
                        int y = MapY + await Kernel.NextAsync(mGenerator.GetHeight());

                        if (FindPath(x, y))
                            await PathMoveAsync(RoleMoveMode.Walk);
                    }
                }
                else
                {
                    var dir = (FacingDirection) (await Kernel.NextAsync(int.MaxValue) % 8);
                    if (await Kernel.NextAsync(100) < 5 && TestPath(dir))
                        await PathMoveAsync(RoleMoveMode.Walk);
                }
            }
            else
            {
                if (IsGuard() || IsPkKiller() || IsEvilKiller() || IsFastBack() || await Kernel.NextAsync(100) < 25)
                    if (mGenerator.IsInRegion(MapX, MapY))
                    {
                        Point pos = mGenerator.GetCenter();
                        if (FindPath(pos.X, pos.Y))
                            await PathMoveAsync(RoleMoveMode.Walk);
                        else
                            await JumpBlockAsync(pos.X, pos.Y, Direction);
                    }
            }

            return true;
        }

        private async Task<bool> OnForwardAsync()
        {
            Role target = RoleManager.GetRole(mMoveTarget);
            if (target != null
                && target.IsAlive
                && target.QueryStatus(StatusSet.INVISIBLE) == null
                && GetDistance(target) <= GetAttackRange(target.SizeAddition))
            {
                if (!IsGuard() && !IsMoveEnable() && IsFarWeapon() && !mAheadPath &&
                    mNextDir != FacingDirection.Invalid)
                    if (await PathMoveAsync(RoleMoveMode.Run))
                        return true;

                await ChangeModeAsync(AiStage.Attack);
                return true;
            }

            // process forward
            if ((IsGuard() || IsPkKiller() || IsFastBack()) && mGenerator.IsTooFar(MapX, MapY, 48))
            {
                Point pos = mGenerator.GetCenter();

                mActTarget = 0;
                mMoveTarget = 0;

                await FarJumpAsync(pos.X, pos.Y, Direction);
                ClearPath();
                await ChangeModeAsync(AiStage.Idle);
                return true;
            }

            if ((IsGuard() || IsPkKiller() || IsEvilKiller()) && target != null &&
                GetDistance(target.MapX, target.MapY) >= GetAttackRange(target.SizeAddition))
            {
                await JumpPosAsync(target.MapX, target.MapY);
                return true;
            }

            if (mNextDir == FacingDirection.Invalid || target == null || !target.IsAlive)
            {
                if (await FindNewTargetAsync())
                {
                    if (mNextDir == FacingDirection.Invalid)
                    {
                        if (IsJumpEnable())
                        {
                            target = RoleManager.GetRole(mMoveTarget);
                            if (target != null)
                                await JumpBlockAsync(target.MapX, target.MapY, Direction);
                            return true;
                        }

                        await ChangeModeAsync(AiStage.Idle);
                        return true;
                    }

                    return false;
                }

                await ChangeModeAsync(AiStage.Idle);
                return true;
            }

            if (mActTarget != 0)
            {
                FindPath();
                if (mMoveTarget != 0)
                    await PathMoveAsync(RoleMoveMode.Run);
                else
                    await PathMoveAsync(RoleMoveMode.Walk);
            }

            return true;
        }

        private async Task<bool> OnAttackAsync()
        {
            Role target = RoleManager.GetRole(mActTarget);
            if (target != null
                && target.IsAlive
                && GetDistance(target) <= GetAttackRange(target.SizeAddition))
            {
                if (mAction.ToNextTime())
                {
                    if (Map.IsSuperPosition(this))
                    {
                        mAheadPath = false;
                        DetectPath(FacingDirection.Invalid);
                        mAheadPath = true;
                        if (mNextDir != FacingDirection.Invalid)
                            await PathMoveAsync(RoleMoveMode.Shift);
                    }

                    FindPath();
                    await ChangeModeAsync(AiStage.Forward);
                }

                return true;
            }

            if (await FindNewTargetAsync())
            {
                target = RoleManager.GetRole(mMoveTarget);
                if (target != null
                    && target.IsAlive
                    && GetDistance(target) > GetAttackRange(target.SizeAddition))
                {
                    if (mNextDir != FacingDirection.Invalid && IsMoveEnable())
                    {
                        await ChangeModeAsync(AiStage.Forward);
                        return true;
                    }

                    await ChangeModeAsync(AiStage.Idle);
                    return true;
                }

                return true;
            }

            await ChangeModeAsync(AiStage.Idle);

            return true;
        }

        private async Task<bool> OnEscapeAsync()
        {
            if (!IsEscapeEnable())
            {
                await ChangeModeAsync(AiStage.Idle);
                return false;
            }

            Role target = RoleManager.GetRole(mActTarget);
            if ((IsGuard() || IsPkKiller()) && target != null)
            {
                await JumpPosAsync(target.MapX, target.MapY);
                await ChangeModeAsync(AiStage.Forward);
                return true;
            }

            if (mNextDir == FacingDirection.Invalid)
                FindPath(ViewRange * 2);

            if (mActTarget == 0)
            {
                await ChangeModeAsync(AiStage.Idle);
                return true;
            }

            if (mActTarget != 0 && mNextDir == FacingDirection.Invalid)
            {
                await ChangeModeAsync(AiStage.Forward);
                return true;
            }

            await PathMoveAsync(RoleMoveMode.Run);
            return true;
        }

        private async Task ChangeModeAsync(AiStage mode)
        {
            switch (mode)
            {
                case AiStage.Attack:
                    await DoAttackAsync();
                    break;
            }

            if (mode != AiStage.Forward) ClearPath();

            mStage = mode;
        }

        private async Task DoAttackAsync()
        {
            if (mActTarget == 0)
                return;

            Role target = RoleManager.GetRole(mActTarget);
            if (target == null)
            {
                mActTarget = mMoveTarget = 0;
                return;
            }

            MsgInteract msg;
            if (mDbMonster.MagicType != 0 && mMagics.Count == 1) // only has this magic
                if (await Kernel.NextAsync(100) < mDbMonster.MagicHitrate)
                {
                    msg = new MsgInteract();
                    msg.Action = MsgInteractType.MagicAttack;
                    msg.SenderIdentity = Identity;
                    msg.TargetIdentity = target.Identity;
                    msg.PosX = target.MapX;
                    msg.PosY = target.MapY;
                    msg.Data = (int) mDbMonster.MagicType;
                    await Kernel.SendAsync(msg);
                    return;
                }

            MonsterMagic magic = mMagics.Where(x => x.IsReady()).OrderBy(x => x.LastTick).FirstOrDefault();
            if (magic != null)
            {
                msg = new MsgInteract();
                msg.Action = MsgInteractType.MagicAttack;
                msg.SenderIdentity = Identity;
                msg.TargetIdentity = target.Identity;
                msg.PosX = target.MapX;
                msg.PosY = target.MapY;
                msg.Data = (int) magic.MagicType;
                await Kernel.SendAsync(msg);
                magic.Use();
                return;
            }

            msg = new MsgInteract();
            msg.Action = MsgInteractType.Attack;
            msg.SenderIdentity = Identity;
            msg.TargetIdentity = target.Identity;
            msg.PosX = target.MapX;
            msg.PosY = target.MapY;
            await Kernel.SendAsync(msg);
        }

        #endregion

        #region AI Movement

        private FacingDirection mNextDir = FacingDirection.Invalid;
        private bool mAheadPath;

        private bool DetectPath(FacingDirection noDir)
        {
            ClearPath();

            var posTarget = new Point();
            if (mMoveTarget != 0)
            {
                Role role = RoleManager.GetRole(mMoveTarget);

                if (role == null)
                    return false;

                posTarget.X = role.MapX;
                posTarget.Y = role.MapY;
            }
            else
            {
                posTarget = mGenerator.GetCenter();
            }

            int oldDist = GetDistance(posTarget.X, posTarget.Y);
            int bestDist = oldDist;
            var bestDir = FacingDirection.Invalid;
            var firstDir = FacingDirection.Begin;

            for (var i = FacingDirection.Begin; i < FacingDirection.Invalid; i++)
            {
                FacingDirection dir = firstDir;
                if (dir != noDir)
                {
                    int x = MapX + GameMapData.WalkXCoords[(int) dir];
                    int y = MapY + GameMapData.WalkYCoords[(int) dir];
                    if (Map.IsMoveEnable(x, y, dir, SizeAddition, NPC_CLIMBCAP))
                    {
                        int dist = GetDistance(x, y);
                        if (bestDist - dist * (mAheadPath ? 1 : -1) > 0)
                        {
                            bestDist = dist;
                            bestDir = dir;
                        }
                    }
                }
            }

            if (bestDir != FacingDirection.Invalid)
            {
                mNextDir = bestDir;
                return true;
            }

            return true;
        }

        private bool FindPath(int x, int y)
        {
            if (x == MapX && y == MapY)
                return false;

            var dir = (FacingDirection) Calculations.GetDirection(MapX, MapY, x, y);
            if (!mAheadPath)
                dir += 4;
            for (var i = 0; i < 8; i++)
            {
                dir = (FacingDirection) (((int) dir + i) % 8);
                if (TestPath(dir))
                {
                    mNextDir = dir;
                    return true;
                }
            }

            return mNextDir != FacingDirection.Invalid;
        }

        private bool FindPath(int scapeSteps = 0)
        {
            if (mMoveTarget == 0)
                return false;

            mAheadPath = scapeSteps == 0;
            ClearPath();

            Role role = Map.QueryAroundRole(this, mMoveTarget);
            if (role == null || !role.IsAlive || GetDistance(role) > ViewRange && mAheadPath)
            {
                mMoveTarget = 0;
                mActTarget = 0;
                return false;
            }

            if (!FindPath(role.MapX, role.MapY))
            {
                mMoveTarget = 0;
                mActTarget = 0;
                return false;
            }

            if (mNextDir != FacingDirection.Invalid)
                if (!Map.IsMoveEnable(MapX, MapY, mNextDir, SizeAddition))
                {
                    DetectPath(mNextDir);
                    return mNextDir != FacingDirection.Invalid;
                }

            return mNextDir != FacingDirection.Invalid;
        }

        private bool TestPath(FacingDirection dir)
        {
            if (dir == FacingDirection.Invalid)
                return false;

            int x = MapX + GameMapData.WalkXCoords[(int) dir];
            int y = MapY + GameMapData.WalkYCoords[(int) dir];

            if (Map.IsMoveEnable(x, y, dir, SizeAddition, NPC_CLIMBCAP))
            {
                mNextDir = dir;
                return true;
            }

            return false;
        }

        public void ClearPath()
        {
            mNextDir = FacingDirection.Invalid;
        }

        private async Task<bool> PathMoveAsync(RoleMoveMode mode)
        {
            if (mode == RoleMoveMode.Walk)
            {
                if (!mMove.ToNextTime(mDbMonster.MoveSpeed))
                    return true;
            }
            else
            {
                if (!mMove.ToNextTime((int) mDbMonster.RunSpeed))
                    return true;
            }

            //int newX = MapX + GameMapData.WalkXCoords[(int) mNextDir];
            //int newY = MapY + GameMapData.WalkYCoords[(int) mNextDir];

            //if (!Map.IsSuperPosition(newX, newY))
            {
                if (TestPath(mNextDir))
                {
                    await MoveTowardAsync((int) mNextDir, (int) mode);
                    return true;
                }
            }

            if (DetectPath(mNextDir) /* && !Map.IsSuperPosition(newX, newY)*/)
            {
                await MoveTowardAsync((int) mNextDir, (int) mode);
                return true;
            }

            if (IsJumpEnable())
            {
                Point pos = mGenerator.GetCenter();
                await JumpBlockAsync(pos.X, pos.Y, Direction);
                return true;
            }

            return false;
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

        public const int NPC_CLIMBCAP = 26;
        public const int SHORTWEAPON_RANGE_LIMIT = 2; // ½üÉíÎäÆ÷µÄ×î´ó¹¥»÷·¶Î§(ÒÔ´Ë·¶Î§ÊÇ·ñ¹­¼ýÊÖ)

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
}