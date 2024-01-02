using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.World.Maps;
using Comet.Core.World.Maps.Enums;
using Comet.Game.Packets;
using Comet.Game.Packets.Ai;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Game;
using Comet.Shared;
using static Comet.Network.Packets.Game.MsgWalk<Comet.Game.States.Client>;
using MsgAction = Comet.Game.Packets.MsgAction;
using MsgInteract = Comet.Game.Packets.MsgInteract;

namespace Comet.Game.States
{
    public sealed partial class MagicData
    {
        private const int MAX_TARGET_NUM = 25;
        private const int TC_PK_ARENA_ID = 1005;

        private readonly TimeOutMS mDelay = new(MAGIC_DELAY);
        private readonly TimeOutMS mIntone = new();
        private readonly TimeOutMS mMagicDelay = new(MAGIC_DELAY);

        private ushort mTypeMagic;

        private Point mTargetPos;
        private uint mIdTarget;

        private bool mAutoAttack;
        private int mAutoAttackNum;

        public MagicState State { get; private set; } = MagicState.None;

        public async Task<(bool Success, ushort X, ushort Y)> CheckConditionAsync(Magic magic, uint idTarget, ushort x,
            ushort y)
        {
            int delay = mOwner.Map.IsTrainingMap()
                            ? MAGIC_DELAY
                            : MAGIC_DELAY - magic.Level * MAGIC_DECDELAY_PER_LEVEL;
            if (!mMagicDelay.IsTimeOut(delay) &&
                magic.Sort != MagicSort.Collide)
                return (false, x, y);

            if (!magic.IsReady())
                return (false, x, y);

            if (mOwner.Map.IsLineSkillMap() && magic.Sort != MagicSort.Line)
                return (false, x, y);

            if (!((magic.AutoActive & 1) == 1
                  || (magic.AutoActive & 4) == 4) && magic.Type != 6001)
                if (!await Kernel.ChanceCalcAsync(magic.Percent))
                    return (false, x, y);

            GameMap map = mOwner.Map;
            var user = mOwner as Character;
            Role role = null;
            if (user != null && user.Map.QueryRegion(RegionTypes.PkProtected, user.MapX, user.MapY))
            {
                if (magic.Ground > 0)
                {
                    if (magic.Crime > 0)
                        return (false, x, y);
                }
                else
                {
                    role = map.QueryAroundRole(user, idTarget);
                    if (role is Character && magic.Crime > 0)
                        return (false, x, y);
                }
            }

            if (map.IsLineSkillMap() && magic.Sort != MagicSort.Line)
                return (false, x, y);

            if (user?.CurrentEvent != null && !user.CurrentEvent.IsAttackEnable(user) &&
                magic.Sort != MagicSort.Attachstatus)
                return (false, x, y);

            if (!map.IsTrainingMap() && user != null)
            {
                if (user.Mana < magic.UseMana)
                    return (false, x, y);
                if (user.Energy < magic.UseStamina)
                    return (false, x, y);

                if (magic.UseItem > 0 && user.CheckWeaponSubType(magic.UseItem, magic.UseItemNum))
                    return (false, x, y);
            }

            if (magic.UseXp == BattleSystem.MagicType.Normal)
            {
                IStatus pStatus = mOwner.QueryStatus(StatusSet.START_XP);
                if (pStatus == null && magic.Status != StatusSet.VORTEX)
                    return (false, x, y);
            }

            if (magic.WeaponSubtype > 0 && user != null)
            {
                if (!user.CheckWeaponSubType(magic.WeaponSubtype))
                    return (false, x, y);

                if (magic.Type == TWOFOLDBLADES_ID
                    && user.UserPackage[Item.ItemPosition.RightHand].GetItemSubType() !=
                    user.UserPackage[Item.ItemPosition.LeftHand]?.GetItemSubType())
                    return (false, x, y);
            }

            if (user != null && user.TransformationMesh != 0)
                return (false, x, y);

            if (mOwner.IsWing && magic.Sort == MagicSort.Transform)
                return (false, x, y);

            if (map.IsWingDisable() && magic.Sort == MagicSort.Attachstatus && magic.Status == StatusSet.FLY)
                return (false, x, y);

            if (magic.Ground == 0 && magic.Sort != MagicSort.Groundsting
                                  && magic.Sort != MagicSort.Vortex
                                  && magic.Sort != MagicSort.Dashwhirl
                                  && magic.Sort != MagicSort.Dashdeadmark
                                  && magic.Sort != MagicSort.Mountwhirl)
            {
                role = map.QueryAroundRole(mOwner, idTarget);
                if (role == null)
                    return (false, x, y);

                if (!role.IsAlive
                    && magic.Sort != MagicSort.Attachstatus
                    && magic.Sort != MagicSort.Detachstatus)
                    return (false, x, y);

                if (magic.Sort == MagicSort.Declife)
                    if (role.Life * 100 / role.MaxLife >= 15)
                        return (false, x, y);

                x = role.MapX;
                y = role.MapY;
            }

            if (HitByMagic(magic) != 0 && !HitByWeapon() && mOwner.GetDistance(x, y) > magic.Distance + 1)
                return (false, x, y);

            if (mOwner.GetDistance(x, y) > mOwner.GetAttackRange(0) + magic.Distance + 1)
                return (false, x, y);

            if (role is DynamicNpc dyna)
                if (dyna.IsGoal() && mOwner.Level < dyna.Level)
                    return (false, x, y);

            return (true, x, y);
        }

        public async Task<bool> ProcessMagicAttackAsync(ushort usMagicType, uint idTarget, ushort x, ushort y,
                                                        uint ucAutoActive = 0)
        {
            switch (State)
            {
                case MagicState.Intone:
                    await AbortMagicAsync(true);
                    break;
            }

            State = MagicState.None;
            mTypeMagic = usMagicType;

            if (!Magics.TryGetValue(usMagicType, out Magic magic)
                && (ucAutoActive == 0 || (magic?.AutoActive ?? 0 & ucAutoActive) != 0))
            {
                await Log.GmLogAsync(
                    "cheat", $"invalid magic type: {usMagicType}, user[{mOwner.Name}][{mOwner.Identity}]");
                return false;
            }

            if (magic == null)
                return false;

            (bool Success, ushort X, ushort Y) result = await CheckConditionAsync(magic, idTarget, x, y);
            if (!result.Success)
            {
                if (magic.Sort == MagicSort.Collide)
                    await ProcessCollideFailAsync(x, y, (int) idTarget);

                await AbortMagicAsync(true);
                return false;
            }

            /*if (magic.Ground > 0 && magic.Sort != MagicSort.Atkstatus)
                mIdTarget = 0;
            else*/
            mIdTarget = idTarget;

            mTargetPos = new Point(x, y);

            var user = mOwner as Character;
            GameMap map = mOwner.Map;
            if (user != null && !map.IsTrainingMap() && map.Identity != TC_PK_ARENA_ID)
            {
                if (magic.UseMana > 0)
                    await user.AddAttributesAsync(ClientUpdateType.Mana, magic.UseMana * -1);
                if (magic.UseStamina > 0)
                    await user.AddAttributesAsync(ClientUpdateType.Stamina, magic.UseStamina * -1);
                if (magic.UseItemNum > 0)
                    await user.SpendEquipItemAsync(magic.WeaponSubtype, magic.UseItemNum, true);
                if (magic.UseItemNum > 0 && user.UserPackage[Item.ItemPosition.RightHand]?.IsBow() == true)
                    await user.SpendEquipItemAsync(50, magic.UseItemNum, true);
            }

            if (magic.UseXp == BattleSystem.MagicType.Normal && user != null)
            {
                if (magic.Status == StatusSet.VORTEX && mOwner.QueryStatus(StatusSet.VORTEX) != null)
                {
                }
                else
                {
                    IStatus pStatus = mOwner.QueryStatus(StatusSet.START_XP);
                    if (pStatus == null)
                    {
                        await AbortMagicAsync(true);
                        return false;
                    }

                    await user.DetachStatusAsync(StatusSet.START_XP);
                    await user.ClsXpValAsync();
                }
            }

            if (!IsWeaponMagic(magic.Type))
                await mOwner.BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.MagicAttack,
                    TargetIdentity = idTarget,
                    SenderIdentity = mOwner.Identity,
                    PosX = mOwner.MapX,
                    PosY = mOwner.MapY
                }, true);

            mTypeMagic = magic.Type; // for auto attack!

            if (magic.UseMana != 0)
            {
                if (!map.IsTrainingMap() && user != null)
                    await user.DecEquipmentDurabilityAsync(false, (int) HitByMagic(magic), (ushort) magic.UseItemNum);

                if (await Kernel.ChanceCalcAsync(7) && user != null)
                    await user.SendGemEffectAsync();
            }

            mMagicDelay.Update();
            await mOwner.ProcessOnAttackAsync();

            if (magic.IntoneSpeed <= 0)
            {
                if (!await LaunchAsync(magic)) // pode ocorrer caso o monstro desapareça, morra antes da hora
                {
                    ResetDelay();
                }
                else
                {
                    if (mOwner.Map.IsTrainingMap() || IsAutoAttack())
                    {
                        SetAutoAttack(magic.Type);
                        mDelay.Startup(Math.Max(MAGIC_DELAY, magic.DelayMs));
                        State = MagicState.Delay;
                        return true;
                    }

                    State = MagicState.None;
                }
            }
            else
            {
                State = MagicState.Intone;
                mIntone.Startup((int) magic.IntoneSpeed);
            }

            return true;
        }

        #region Processing

        private async Task<bool> LaunchAsync(Magic magic)
        {
            var result = false;
            try
            {
                if (magic == null)
                    return false;

                if (!mOwner.IsAlive)
                    return false;

                magic.Use();

                switch (magic.Sort)
                {
                    case MagicSort.Attack:
                        result = await ProcessAttackAsync(magic);
                        break;
                    case MagicSort.Recruit:
                        result = await ProcessRecruitAsync(magic);
                        break;
                    case MagicSort.Fan:
                        result = await ProcessFanAsync(magic);
                        break;
                    case MagicSort.Bomb:
                        result = await ProcessBombAsync(magic);
                        break;
                    case MagicSort.Attachstatus:
                        result = await ProcessAttachAsync(magic);
                        break;
                    case MagicSort.Detachstatus:
                        result = await ProcessDetachAsync(magic);
                        break;
                    case MagicSort.Dispatchxp:
                        result = await ProcessDispatchXpAsync(magic);
                        break;
                    case MagicSort.Line:
                        result = await ProcessLineAsync(magic);
                        break;
                    case MagicSort.Atkstatus:
                        result = await ProcessAttackStatusAsync(magic);
                        break;
                    case MagicSort.Transform:
                        result = await ProcessTransformAsync(magic);
                        break;
                    case MagicSort.Addmana:
                        result = await ProcessAddManaAsync(magic);
                        break;
                    case MagicSort.Callpet:
                        result = await ProcessCallPetAsync(magic);
                        break;
                    case MagicSort.Groundsting:
                        result = await ProcessGroundStingAsync(magic);
                        break;
                    case MagicSort.Vortex:
                        result = await ProcessVortexAsync(magic);
                        break;
                    case MagicSort.Activateswitch:
                        result = await ProcessActivateSwitchAsync(magic);
                        break;
                    case MagicSort.Spook:
                        result = await ProcessSpookAsync(magic);
                        break;
                    case MagicSort.Warcry:
                        result = await ProcessWarCryAsync(magic);
                        break;
                    case MagicSort.Riding:
                        result = await ProcessRidingAsync(magic);
                        break;

                    default:
                        await Log.WriteLogAsync(LogLevel.Warning,
                                                $"MagicProcessing::LaunchAsync {magic.Sort} not handled!!!");
                        result = true;
                        break;
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error ocurred on MagicProcessing::LaunchAsync");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            mAutoAttackNum++;
            return result;
        }

        private async Task<bool> ProcessAttackAsync(Magic magic)
        {
            if (magic == null || mOwner == null || mIdTarget == 0)
                return false;

            Role targetRole = mOwner.Map.QueryAroundRole(mOwner, mIdTarget);
            if (targetRole == null
                || mOwner.GetDistance(targetRole) > magic.Distance
                || !targetRole.IsAlive
                || !targetRole.IsAttackable(mOwner))
                return false;

            if (mOwner.IsImmunity(targetRole))
                return false;

            if (magic.FloorAttr > 0)
            {
                int nAttr = targetRole.Map[targetRole.MapX, targetRole.MapY].Elevation;
                if (nAttr != magic.FloorAttr)
                    return false;
            }

            BattleSystem.MagicType byMagic = HitByMagic(magic);
            (int Damage, InteractionEffect effect) result =
                await mOwner.BattleSystem.CalcPowerAsync(byMagic, mOwner, targetRole, magic.Power);
            int power = result.Damage;

            var user = mOwner as Character;
            if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
            {
                await user.SendEffectAsync("LuckyGuy", true);
                power *= 2;
            }

            if (mOwner.IsCallPet())
            {
                user = RoleManager.GetUser(mOwner.OwnerIdentity);
            }

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY
            };
            msg.Append(targetRole.Identity, power, true);
            await mOwner.BroadcastRoomMsgAsync(msg, true);

            await CheckCrimeAsync(targetRole, magic);

            var totalExp = 0;
            if (power > 0 && !await targetRole.CheckScapegoatAsync(mOwner))
            {
                var lifeLost = (int) Math.Min(targetRole.MaxLife, power);
                await targetRole.BeAttackAsync(byMagic, mOwner, power, true);
                totalExp = lifeLost;

                if (user != null && targetRole is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                    dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);

                if (user?.CurrentEvent != null)
                    await user.CurrentEvent.OnHitAsync(user, targetRole, magic);
            }

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            if (totalExp > 0) await AwardExpOfLifeAsync(targetRole, totalExp, magic);

            if (!targetRole.IsAlive)
            {
                var nBonusExp = (int) (targetRole.MaxLife * 20 / 100);
                if (user != null)
                    await user.BattleSystem.OtherMemberAwardExpAsync(targetRole, nBonusExp);
                await mOwner.KillAsync(targetRole, GetDieMode());
            }
            else
            {
                if ((targetRole is Monster monster && !monster.IsGuard()
                     || targetRole is DynamicNpc)
                    && byMagic == BattleSystem.MagicType.Normal)
                    SetAutoAttack(magic.Type);
            }

            if (user != null)
                await user.SendWeaponMagic2Async(targetRole);

            return true;
        }

        private async Task<bool> ProcessRecruitAsync(Magic magic)
        {
            if (magic == null || mOwner == null || mIdTarget == 0)
                return false;

            var setTarget = new List<Role>();
            var team = mOwner.GetTeam();
            if (team != null && magic.Multi != 0)
            {
                foreach (var member in team.Members)
                {
                    if (!member.IsAlive 
                        || (!member.IsPlayer() && !member.IsMonster())
                        || mOwner.GetDistance(member) > Screen.VIEW_SIZE)
                        continue;

                    setTarget.Add(member);
                }
            }
            else
            {
                Role targetRole = mOwner.Map.QueryAroundRole(mOwner, mIdTarget);

                if (targetRole == null
                    || mOwner.GetDistance(targetRole) > magic.Distance
                    || !targetRole.IsAlive)
                    return false;

                setTarget.Add(targetRole);
            }

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY
            };

            var exp = 0;
            foreach (Role target in setTarget)
            {
                if (!target.IsAlive)
                    continue;

                var power = (int) Math.Min(magic.Power, target.MaxLife - target.Life);
                if (power == Calculations.ADJUST_FULL)
                    power = (int) (target.MaxLife - target.Life);

                exp += power;

                msg.Append(target.Identity, power, false);

                if (power > 0)
                {
                    await target.AddAttributesAsync(ClientUpdateType.Hitpoints, power);

                    if (target is Character user)
                        await user.BroadcastTeamLifeAsync();
                }
            }

            if (mOwner.Map.IsTrainingMap())
                exp = Math.Max(exp, magic.Power);

            await mOwner.BroadcastRoomMsgAsync(msg, true);
            await AwardExpAsync(0, exp, true);
            return true;
        }

        private async Task<bool> ProcessFanAsync(Magic magic)
        {
            int nRange = (int) magic.Distance + 2;
            const int WIDTH = DEFAULT_MAGIC_FAN;
            long nExp = 0, battleExp = 0;

            var setTarget = new List<Role>();
            var center = new Point(mOwner.MapX, mOwner.MapY);

            Role tgt = mOwner.Map.QueryAroundRole(mOwner, mIdTarget);
            if (tgt != null && tgt.IsAlive)
                setTarget.Add(tgt);

            List<Role> targets = mOwner.Map.Query9BlocksByPos(mOwner.MapX, mOwner.MapY);
            foreach (Role target in targets)
            {
                if (target.Identity == mOwner.Identity)
                    continue;

                var posThis = new Point(target.MapX, target.MapY);
                if (!Calculations.IsInFan(center, mTargetPos, posThis, WIDTH, nRange))
                    continue;

                if (target.IsAttackable(mOwner)
                    && !mOwner.IsImmunity(target)
                    && target.Identity != mIdTarget)
                    setTarget.Add(target);
            }

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = (ushort) mTargetPos.X,
                MapY = (ushort) mTargetPos.Y,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };

            var user = mOwner as Character;
            BattleSystem.MagicType byMagic = HitByMagic(magic);
            var bMagic2Dealt = false;
            foreach (Role target in setTarget)
            {
                if (!target.IsAttackable(mOwner)
                    || mOwner.IsImmunity(target))
                    continue;

                (int damage, InteractionEffect effect) =
                    await mOwner.BattleSystem.CalcPowerAsync(byMagic, mOwner, target, magic.Power);
                if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 250))
                {
                    await user.SendEffectAsync("LuckyGuy", true);
                    damage *= 2;
                }

                if (msg.Count >= MAX_TARGET_NUM)
                {
                    await mOwner.BroadcastRoomMsgAsync(msg, true);
                    msg.ClearTargets();
                }

                msg.Append(target.Identity, damage, true);

                if (!await target.CheckScapegoatAsync(mOwner))
                {
                    var lifeLost = (int) Math.Min(target.Life, damage);
                    await target.BeAttackAsync(byMagic, mOwner, lifeLost, true);

                    if (user != null && target is Monster monster)
                    {
                        nExp += lifeLost;
                        battleExp += user.AdjustExperience(monster, lifeLost, false);
                        if (!monster.IsAlive)
                        {
                            var nBonusExp = (int) (monster.MaxLife * 20 / 100d);

                            if (user.Team != null)
                                await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);

                            nExp += user.AdjustExperience(monster, nBonusExp, false);
                        }
                    }

                    if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                        dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);

                    if (user?.CurrentEvent != null)
                        await user.CurrentEvent.OnHitAsync(user, target, magic);

                    if (!target.IsAlive)
                        await mOwner.KillAsync(target, GetDieMode());
                }

                if (!bMagic2Dealt && await Kernel.ChanceCalcAsync(5d) && user != null)
                {
                    await user.SendWeaponMagic2Async(target);
                    bMagic2Dealt = true;
                }
            }

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            await mOwner.BroadcastRoomMsgAsync(msg, true);
            await CheckCrimeAsync(setTarget.ToDictionary(x => x.Identity), magic);
            await AwardExpAsync(battleExp, nExp, false, magic);
            return true;
        }

        private async Task<bool> ProcessBombAsync(Magic magic)
        {
            if (magic == null || mOwner == null)
                return false;

            var setTarget = new List<Role>();

            (List<Role> Roles, Point Center) result = CollectTargetBomb(0, (int) magic.Range);

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = (ushort) result.Center.X,
                MapY = (ushort) result.Center.Y,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };

            long battleExp = 0;
            long exp = 0;
            var user = mOwner as Character;
            foreach (Role target in result.Roles)
            {
                if (magic.Ground != 0 && target.IsWing)
                    continue;

                (int Damage, InteractionEffect effect) atkResult =
                    await mOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), mOwner, target, magic.Power);

                if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
                {
                    await user.SendEffectAsync("LuckyGuy", true);
                    atkResult.Damage *= 2;
                }

                if (!await target.CheckScapegoatAsync(mOwner))
                {
                    var lifeLost = (int) Math.Min(atkResult.Damage, target.Life);

                    await target.BeAttackAsync(HitByMagic(magic), mOwner, atkResult.Damage, true);

                    if (user != null && target is Monster monster)
                    {
                        exp += lifeLost;
                        battleExp += user.AdjustExperience(target, lifeLost, false);
                        if (!monster.IsAlive)
                        {
                            var nBonusExp = (int) (monster.MaxLife * 20 / 100d);
                            if (user.Team != null)
                                await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);
                            battleExp += user.AdjustExperience(monster, nBonusExp, false);
                        }
                    }

                    if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                        dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);

                    if (user?.CurrentEvent != null)
                        await user.CurrentEvent.OnHitAsync(user, target, magic);

                    if (!target.IsAlive)
                        await mOwner.KillAsync(target, GetDieMode());
                }

                if (msg.Count < MAX_TARGET_NUM)
                    msg.Append(target.Identity, atkResult.Damage, true);
            }

            await mOwner.Map.BroadcastRoomMsgAsync(result.Center.X, result.Center.Y, msg);

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            await CheckCrimeAsync(result.Roles.ToDictionary(x => x.Identity, x => x), magic);
            await AwardExpAsync(0, battleExp, exp, magic);
            return true;
        }

        private async Task<bool> ProcessAttachAsync(Magic magic)
        {
            if (magic == null)
                return false;

            Role target = mOwner.Map.QueryRole(mIdTarget);
            if (target == null)
                return false;

            /*
             * 64 can only be used on dead players
             */
            if (!target.IsAlive && magic.Target != 64 && !(target is Character))
                return false;

            int power = magic.Power;
            var secs = (int) magic.StepSeconds;
            var times = (int) magic.ActiveTimes;
            var status = (int) magic.Status;
            var level = (byte) magic.Level;

            if (power < 0)
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Error magic type invalid power {magic.Type} {magic.Power}");
                return false;
            }

            if (secs <= 0)
                secs = int.MaxValue;

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };

            var damage = 1;
            switch (status)
            {
                case StatusSet.FLY:
                {
                    if (target.Identity != mOwner.Identity)
                        return false;
                    if (!target.IsBowman || !target.IsAlive)
                        return false;
                    if (target.Map.IsWingDisable())
                        return false;
                    if (target.QueryStatus(StatusSet.SHIELD) != null)
                        return false;
                    if (target.QueryStatus(StatusSet.RIDING) != null)
                        return false;
                    break;
                }

                case StatusSet.LUCKY_DIFFUSE:
                {
                    damage = 0;
                    break;
                }

                case StatusSet.POISON_STAR:
                {
                    int chance = 100 - Math.Min(20, Math.Max(0, target.BattlePower - mOwner.BattlePower)) * 5;
                    if (!await Kernel.ChanceCalcAsync(chance))
                    {
                        msg.Append(target.Identity, 0, false);
                        await mOwner.BroadcastRoomMsgAsync(msg, true);
                        return true;
                    }

                    break;
                }
            }

            msg.Append(target.Identity, damage, damage != 0);
            await mOwner.BroadcastRoomMsgAsync(msg, true);

            await CheckCrimeAsync(target, magic);

            await target.AttachStatusAsync(mOwner, status, power, secs, times, level);

            if (power >= Calculations.ADJUST_PERCENT)
            {
                int powerTimes = power - 30000 - 100;
                switch (status)
                {
                    case StatusSet.STAR_OF_ACCURACY:
                        await target.SendAsync(string.Format(Language.StrAccuracyActiveP, secs, powerTimes));
                        break;
                    case StatusSet.DODGE:
                        await target.SendAsync(string.Format(Language.StrDodgeActiveP, secs, powerTimes));
                        break;
                    case StatusSet.STIGMA:
                        await target.SendAsync(string.Format(Language.StrStigmaActiveP, secs, powerTimes));
                        break;
                    case StatusSet.SHIELD:
                        await target.SendAsync(string.Format(Language.StrShieldActiveP, secs, powerTimes));
                        break;
                }
            }
            else
            {
                switch (status)
                {
                    case StatusSet.STAR_OF_ACCURACY:
                        await target.SendAsync(string.Format(Language.StrAccuracyActiveT, secs, power));
                        break;
                    case StatusSet.DODGE:
                        await target.SendAsync(string.Format(Language.StrDodgeActiveT, secs, power));
                        break;
                    case StatusSet.STIGMA:
                        await target.SendAsync(string.Format(Language.StrStigmaActiveT, secs, power));
                        break;
                    case StatusSet.SHIELD:
                        await target.SendAsync(string.Format(Language.StrShieldActiveT, secs, power));
                        break;
                }
            }

            if (mOwner is Character)
                await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessDetachAsync(Magic magic)
        {
            if (magic == null) return false;

            Role target = mOwner.Map.QueryRole(mIdTarget);
            if (target == null)
                return false;

            int power = magic.Power;
            var secs = (int) magic.StepSeconds;
            var times = (int) magic.ActiveTimes;
            var status = (int) magic.Status;
            var level = (byte) magic.Level;

            if (!target.IsAlive && target.IsPlayer())
            {
                if (status != -1)
                    return false;

                if (target.Map.IsPkField())
                    return false;
            }

            if (status == -1 && target is Character user)
            {
                await user.RebornAsync(false, true);
                await user.Map.SendMapInfoAsync(user);
            }

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };

            switch (magic.Status)
            {
                case StatusSet.FLY:
                {
                    if (!target.IsWing)
                        return true;

                    int chance = 100 - Math.Min(20, Math.Max(0, target.BattlePower - mOwner.BattlePower)) * 5;
                    if (!await Kernel.ChanceCalcAsync(chance))
                    {
                        msg.Append(target.Identity, 0, false);
                        await mOwner.BroadcastRoomMsgAsync(msg, true);
                        return true;
                    }

                    break;
                }
            }

            msg.Append(target.Identity, power, true);
            await target.BroadcastRoomMsgAsync(msg, true);

            if (power > 0)
            {
                var lifeLost = (int) Math.Min(target.Life, Math.Max(0, Calculations.AdjustData(target.Life, power)));
                await target.BeAttackAsync(HitByMagic(magic), mOwner, lifeLost, true);
                await target.AddAttributesAsync(ClientUpdateType.Hitpoints, lifeLost * -1);
            }

            await target.DetachStatusAsync((int) magic.Status);
            return true;
        }

        private async Task<bool> ProcessDispatchXpAsync(Magic magic)
        {
            if (magic == null)
                return false;

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            if (mOwner is Character user && user.Team != null)
                foreach (Character member in user.Team.Members.Where(x => x.Identity != user.Identity && x.IsAlive))
                {
                    if (mOwner.GetDistance(member) > Screen.VIEW_SIZE * 2)
                        continue;

                    msg.Append(member.Identity, DISPATCHXP_NUMBER, true);
                    await member.SetXpAsync(DISPATCHXP_NUMBER);
                    await member.BurstXpAsync();
                    await member.SendAsync(string.Format(Language.StrDispatchXp, user.Name));
                }

            await mOwner.BroadcastRoomMsgAsync(msg, true);
            await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessLineAsync(Magic magic)
        {
            if (magic == null || mOwner == null)
                return false;

            List<Role> allTargets = mOwner.Map.Query9BlocksByPos(mOwner.MapX, mOwner.MapY);
            var targets = new List<Role>();
            var setPoint = new List<Point>();
            var pos = new Point(mOwner.MapX, mOwner.MapY);
            Calculations.DDALine(pos.X, pos.Y, mTargetPos.X, mTargetPos.Y, (int) magic.Range, ref setPoint);

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = (ushort) mTargetPos.X,
                MapY = (ushort) mTargetPos.Y,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            long exp = 0;
            long battleExp = 0;
            var user = mOwner as Character;

            Tile userTile = mOwner.Map[mOwner.MapX, mOwner.MapY];
            foreach (Point point in setPoint)
            {
                if (msg.Count >= MAX_TARGET_NUM)
                {
                    await mOwner.BroadcastRoomMsgAsync(msg, true);
                    msg.ClearTargets();
                }

                Tile targetTile = mOwner.Map[point.X, point.Y];
                if (userTile.Elevation - targetTile.Elevation > 26)
                    continue;

                Role target = allTargets.FirstOrDefault(x => x.MapX == point.X && x.MapY == point.Y);
                if (target == null || target.Identity == mOwner.Identity)
                    continue;

                if (magic.Ground != 0 && target.IsWing)
                    continue;

                if (!mOwner.Map.IsAltEnable(mOwner.MapX, mOwner.MapY, target.MapX, target.MapY))
                    continue;

                if (mOwner.IsImmunity(target)
                    || !target.IsAttackable(mOwner))
                    continue;

                (int Damage, InteractionEffect effect) result =
                    await mOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), mOwner, target, magic.Power);

                if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
                {
                    await user.SendEffectAsync("LuckyGuy", true);
                    result.Damage *= 2;
                }

                if (!await target.CheckScapegoatAsync(mOwner))
                {
                    var lifeLost = (int) Math.Min(result.Damage, target.Life);

                    await target.BeAttackAsync(HitByMagic(magic), mOwner, result.Damage, true);

                    if (user != null && (target is Monster monster || target is DynamicNpc npc && npc.IsGoal()))
                    {
                        exp += lifeLost;
                        battleExp += user.AdjustExperience(target, lifeLost, false);
                        if (!target.IsAlive)
                        {
                            var nBonusExp = (int) (target.MaxLife * 20 / 100d);
                            if (user.Team != null)
                                await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);
                            battleExp += user.AdjustExperience(target, nBonusExp, false);
                        }
                    }

                    if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                        dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);

                    if (user?.CurrentEvent != null)
                        await user.CurrentEvent.OnHitAsync(user, target, magic);

                    if (!target.IsAlive)
                        await mOwner.KillAsync(target, GetDieMode());
                }

                msg.Append(target.Identity, result.Damage, true);
                targets.Add(target);
            }

            await mOwner.BroadcastRoomMsgAsync(msg, true);

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            await CheckCrimeAsync(targets.ToDictionary(x => x.Identity, x => x), magic);
            await AwardExpAsync(0, battleExp, exp, magic);
            return true;
        }

        private async Task<bool> ProcessAttackStatusAsync(Magic magic)
        {
            if (magic == null)
                return false;

            Role target = mOwner.Map.QueryRole(mIdTarget);
            if (target == null)
                return false;

            if (target.MapIdentity != mOwner.MapIdentity
                || mOwner.GetDistance(target) > magic.Distance + target.SizeAddition)
                return false;

            if (!target.IsAttackable(mOwner) || mOwner.IsImmunity(target))
                return false;

            if (magic.Ground != 0 && target.IsWing)
                return false;

            var power = 0;
            var effect = InteractionEffect.None;

            if (HitByWeapon())
                switch (magic.Status)
                {
                    case 0:
                        break;
                    default:
                        (int Damage, InteractionEffect effect) result =
                            await mOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), mOwner, target, magic.Power);
                        power = result.Damage;
                        effect = result.effect;

                        await target.AttachStatusAsync(mOwner, (int) magic.Status, magic.Power, (int) magic.StepSeconds,
                                                       (int) magic.ActiveTimes, (byte) magic.Level);

                        break;
                }

            var user = mOwner as Character;
            if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
            {
                await user.SendEffectAsync("LuckyGuy", true);
                power *= 2;
            }

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            msg.Append(target.Identity, power, true);
            await mOwner.BroadcastRoomMsgAsync(msg, true);

            long battleExp = 0;
            if (power > 0 && !await target.CheckScapegoatAsync(mOwner))
            {
                var lifeLost = (int) Math.Max(0, Math.Min(target.Life, power));

                await target.BeAttackAsync(HitByMagic(magic), mOwner, power, true);

                if (user != null && target is Monster monster)
                {
                    battleExp += user.AdjustExperience(target, lifeLost, false);
                    if (!monster.IsAlive)
                    {
                        var nBonusExp = (int) (monster.MaxLife * 20 / 100d);

                        if (user.Team != null)
                            await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);

                        battleExp += user.AdjustExperience(monster, nBonusExp, false);
                    }
                }

                if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                    dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);

                if (user?.CurrentEvent != null)
                    await user.CurrentEvent.OnHitAsync(user, target, magic);
            }

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            await AwardExpAsync(0, battleExp, AWARDEXP_BY_TIMES, magic);

            if (!target.IsAlive)
                await mOwner.KillAsync(target, GetDieMode());

            return true;
        }

        private async Task<bool> ProcessTransformAsync(Magic magic)
        {
            if (magic == null || mOwner == null || !(mOwner is Character user))
                return false;

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            await mOwner.BroadcastRoomMsgAsync(msg, true);
            await user.TransformAsync((uint) magic.Power, (int) magic.StepSeconds, true);
            await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessAddManaAsync(Magic magic)
        {
            if (magic == null)
                return false;

            Role target = null;
            if (magic.Target == 2) // self
            {
                target = mOwner;
            }
            else if (magic.Target == 1) // target
            {
                target = mOwner.Map.QueryRole(mIdTarget);
            }
            else // unhandled
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"Add mana unhandled target {magic.Target}");
                return false;
            }

            if (target.Identity != mOwner.Identity
                && (target.MapIdentity != mOwner.MapIdentity || mOwner.GetDistance(target) > magic.Distance))
                return false;

            var addMana = (int) Math.Max(0, Math.Min(target.MaxMana - target.Mana, magic.Power));

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            msg.Append(target.Identity, addMana, true);
            await target.BroadcastRoomMsgAsync(msg, true);

            await target.AddAttributesAsync(ClientUpdateType.Mana, addMana);

            await AwardExpAsync(0, 0, Math.Max(addMana, AWARDEXP_BY_TIMES), magic);
            return true;
        }

        private async Task<bool> ProcessCallPetAsync(Magic magic)
        {
            if (magic == null)
                return false;

            if (mOwner is not Character user || user.Map.IsBoothEnable() || user.Map.IsTrainingMap())
                return false;

            MsgMagicEffect msg = new MsgMagicEffect
            {
                AttackerIdentity = user.Identity,
                MapX = user.MapX,
                MapY = user.MapY,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level
            };
            await user.BroadcastRoomMsgAsync(msg, true);

            await user.CallPetAsync((uint) magic.Power, user.MapX, user.MapY, (int) magic.StepSeconds);

            await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessGroundStingAsync(Magic magic)
        {
            if (magic == null || magic.Status == 0)
                return false;

            (List<Role> Roles, Point Center) targetLocked = CollectTargetBomb(0, (int) magic.Range);
            Point center = targetLocked.Center;
            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = (ushort) center.X,
                MapY = (ushort) center.Y
            };

            var msgSent = false;
            var setTarget = new List<Role>();
            foreach (Role target in targetLocked.Roles)
            {
                if (magic.Ground != 0 && target.IsWing)
                    continue;

                if (mOwner.GetDistance(target) > magic.Distance)
                    continue;

                if (!target.IsPlayer() && !target.IsMonster())
                    continue;

                if (target is Monster targetMonster)
                    if (targetMonster.IsGuard())
                        continue;

                int chance = 100 - Math.Min(20, Math.Max(0, target.BattlePower - mOwner.BattlePower)) * 5;
                var damage = 1;
                if (!await Kernel.ChanceCalcAsync(chance))
                    damage = 0;

                if (msg.Count >= 25)
                {
                    await mOwner.Map.BroadcastRoomMsgAsync(center.X, center.Y, msg);
                    msg = new MsgMagicEffect
                    {
                        AttackerIdentity = mOwner.Identity,
                        MagicIdentity = magic.Type,
                        MagicLevel = magic.Level,
                        MapX = (ushort) center.X,
                        MapY = (ushort) center.Y
                    };
                    msgSent = true;
                }

                msg.Append(target.Identity, damage, true);

                if (damage > 0)
                    setTarget.Add(target);
            }

            if (msg.Count > 0 || !msgSent)
                await mOwner.Map.BroadcastRoomMsgAsync(center.X, center.Y, msg);

            foreach (Role target in setTarget)
                await target.AttachStatusAsync(mOwner, (int) magic.Status, magic.Power, (int) magic.StepSeconds,
                                               (int) magic.ActiveTimes, (byte) magic.Level);

            await AwardExpAsync(0, 0, AWARDEXP_BY_TIMES, magic);
            return true;
        }

        private async Task<bool> ProcessVortexAsync(Magic magic)
        {
            if (!mOwner.IsAlive)
                return false;

            if (mOwner.IsWing)
                return false;

            if (mOwner.QueryStatus(StatusSet.VORTEX) == null)
            {
                await mOwner.AttachStatusAsync(mOwner, (int) magic.Status, magic.Power, (int) magic.StepSeconds,
                                               (int) magic.ActiveTimes, (byte) magic.Level);
            }
            else
            {
                (List<Role> Roles, Point Center) result = CollectTargetBomb(0, (int) magic.Range);

                var msg = new MsgMagicEffect
                {
                    AttackerIdentity = mOwner.Identity,
                    MapX = (ushort) result.Center.X,
                    MapY = (ushort) result.Center.Y,
                    MagicIdentity = magic.Type,
                    MagicLevel = magic.Level
                };

                long battleExp = 0;
                long exp = 0;
                var user = mOwner as Character;
                foreach (Role target in result.Roles)
                {
                    if (magic.Ground != 0 && target.IsWing)
                        continue;

                    (int Damage, InteractionEffect effect) atkResult =
                        await mOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), mOwner, target, magic.Power);

                    if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 100))
                    {
                        await user.SendEffectAsync("LuckyGuy", true);
                        atkResult.Damage *= 2;
                    }

                    if (!await target.CheckScapegoatAsync(mOwner))
                    {
                        var lifeLost = (int) Math.Min(atkResult.Damage, target.Life);

                        await target.BeAttackAsync(HitByMagic(magic), mOwner, atkResult.Damage, true);

                        if (user != null && target is Monster monster)
                        {
                            exp += lifeLost;
                            battleExp += user.AdjustExperience(target, lifeLost, false);
                            if (!monster.IsAlive)
                            {
                                var nBonusExp = (int) (monster.MaxLife * 20 / 100d);
                                if (user.Team != null)
                                    await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);
                                battleExp += user.AdjustExperience(monster, nBonusExp, false);
                            }
                        }

                        if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                            dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);

                        if (user?.CurrentEvent != null)
                            await user.CurrentEvent.OnHitAsync(user, target, magic);

                        if (!target.IsAlive)
                            await mOwner.KillAsync(target, GetDieMode());
                    }

                    if (msg.Count < MAX_TARGET_NUM)
                        msg.Append(target.Identity, atkResult.Damage, true);
                }

                await mOwner.BroadcastRoomMsgAsync(msg, true);

                if (user?.CurrentEvent != null)
                    await user.CurrentEvent.OnAttackAsync(user);

                await CheckCrimeAsync(result.Roles.ToDictionary(x => x.Identity, x => x), magic);
                await AwardExpAsync(0, battleExp, exp, magic);
            }

            return true;
        }

        private async Task<bool> ProcessActivateSwitchAsync(Magic magic)
        {
            if (mIdTarget == 0)
                return false;

            Role target = mOwner.Map.QueryAroundRole(mOwner, mIdTarget);
            if (target == null)
                return false;

            (int damage, InteractionEffect effect) =
                await mOwner.BattleSystem.CalcPowerAsync(HitByMagic(magic), mOwner, target, magic.Power);
            long battleExp = 0;

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = target.MapX,
                MapY = target.MapY
            };
            msg.Append(target.Identity, damage, true);
            await mOwner.BroadcastRoomMsgAsync(msg, true);

            if (damage > 0)
            {
                var lifeLost = (int) Math.Max(0, Math.Min(target.Life, damage));

                await target.BeAttackAsync(HitByMagic(magic), mOwner, damage, true);

                var user = mOwner as Character;
                if (user != null && target is Monster monster)
                {
                    battleExp += user.AdjustExperience(target, lifeLost, false);
                    if (!monster.IsAlive)
                    {
                        var nBonusExp = (int) (monster.MaxLife * 20 / 100d);

                        if (user.Team != null)
                            await user.Team.AwardMemberExpAsync(user.Identity, target, nBonusExp);

                        battleExp += user.AdjustExperience(monster, nBonusExp, false);
                    }
                }

                if (user != null && target is DynamicNpc dynaNpc && dynaNpc.IsAwardScore())
                    dynaNpc.AddSynWarScore(user.Syndicate, lifeLost);

                if (user?.CurrentEvent != null)
                    await user.CurrentEvent.OnHitAsync(user, target, magic);
            }

            if (!target.IsAlive)
                await mOwner.KillAsync(target, GetDieMode());

            if (battleExp > 0)
                await AwardExpAsync(0, battleExp, AWARDEXP_BY_TIMES, magic);

            return true;
        }

        private async Task<bool> ProcessSpookAsync(Magic magic)
        {
            if (magic == null)
                return false;

            int steedPoints = int.MaxValue;
            if (mOwner is Character user)
            {
                Item mount = user.Mount;
                if (mount == null)
                    return false;

                if (mOwner.QueryStatus(StatusSet.RIDING) == null)
                    return false;

                steedPoints = Item.AdditionPoints(mount);
            }

            Character target = RoleManager.GetUser(mIdTarget);
            if (target == null || target.Identity == mOwner.Identity || mOwner.GetDistance(target) > magic.Distance)
                return false;

            if (target.Mount == null || target.QueryStatus(StatusSet.RIDING) == null)
                return false;

            if (steedPoints < Item.AdditionPoints(target.Mount))
                return false;

            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = target.MapX,
                MapY = target.MapY
            };
            msg.Append(target.Identity, 0, true);
            await mOwner.BroadcastRoomMsgAsync(msg, true);

            await target.DetachStatusAsync(StatusSet.RIDING);
            return true;
        }

        private async Task<bool> ProcessWarCryAsync(Magic magic)
        {
            if (magic == null)
                return false;

            int steedPoints = int.MaxValue;
            if (mOwner is Character user)
            {
                Item mount = user.Mount;
                if (mount == null)
                    return false;

                if (mOwner.QueryStatus(StatusSet.RIDING) == null)
                    return false;

                steedPoints = Item.AdditionPoints(mount);
            }

            List<Role> setTarget = new List<Role>();
            var targets = mOwner.Map.Query9Blocks(mOwner.MapX, mOwner.MapY);
            var msg = new MsgMagicEffect
            {
                AttackerIdentity = mOwner.Identity,
                MagicIdentity = magic.Type,
                MagicLevel = magic.Level,
                MapX = mOwner.MapX,
                MapY = mOwner.MapY
            };
            foreach (var target in targets)
            {
                if (target.Identity == mOwner.Identity || target is not Character targetUser)
                    continue;

                if (mOwner.GetDistance(targetUser) > magic.Distance)
                    continue;

                if (targetUser.Mount == null || target.QueryStatus(StatusSet.RIDING) == null)
                    continue;

                if (steedPoints < Item.AdditionPoints(targetUser.Mount))
                    continue;

                msg.Append(target.Identity, 0, true);
                setTarget.Add(target);
            }
            await mOwner.BroadcastRoomMsgAsync(msg, true);

            foreach (var target in setTarget.Cast<Character>())
            {
                await target.DetachStatusAsync(StatusSet.RIDING);
            }

            return true;
        }

        private async Task<bool> ProcessRidingAsync(Magic magic)
        {
            if (mOwner is not Character user)
                return false;

            Item mount = user.UserPackage[Item.ItemPosition.Steed];
            if (mount == null)
                return false;

            if (user.QueryStatus(StatusSet.RIDING) != null)
            {
                await user.DetachStatusAsync(StatusSet.RIDING);
                return true;
            }

            if (user.QueryStatus(StatusSet.FLY) != null)
                return false;

            if (user.Map.IsTrainingMap())
                return false;

            if (user.Map.QueryRegion(RegionTypes.City, user.MapX, user.MapY) && mount.Plus < 4)
                return false;

            if (user.Map.IsBoothEnable() && mount.Plus < 6)
                return false;

            await user.AttachStatusAsync(user, StatusSet.RIDING, 0, (int) magic.StepSeconds, 0, 0);
            await user.SetAttributesAsync(ClientUpdateType.Vigor, (ulong) user.MaxVigor);
            user.UpdateVigorTimer();
            return true;
        }

        private async Task<bool> ProcessCollideFailAsync(ushort x, ushort y, int nDir)
        {
            var nTargetX = (ushort) (x + GameMapData.WalkXCoords[nDir]);
            var nTargetY = (ushort) (y + GameMapData.WalkYCoords[nDir]);

            if (!mOwner.Map.IsStandEnable(nTargetX, nTargetY))
            {
                if (mOwner is Character owner)
                {
                    await owner.SendAsync(Language.StrInvalidMsg);
                    await RoleManager.KickOutAsync(owner.Identity, "INVALID COORDINATES ProcessCollideFail");
                }

                return false;
            }

            var pMsg = new MsgInteract
            {
                SenderIdentity = mOwner.Identity,
                TargetIdentity = 0,
                PosX = nTargetX,
                PosY = nTargetY,
                Action = MsgInteractType.Dash,
                Data = nDir * 0x01000000
            };

            await mOwner.BroadcastRoomMsgAsync(pMsg, true);
            if (mOwner is Character character)
            {
                await character.ProcessOnMoveAsync();
                await character.MoveTowardAsync(nDir, (int) RoleMoveMode.Collide);
            }

            return true;
        }

        #endregion

        #region Magic Processing Manage

        private void ResetDelay()
        {
            if (!Magics.TryGetValue(mTypeMagic, out Magic magic))
                return;
            State = MagicState.Delay;
            mDelay.Update();
            magic.SetDelay();
        }

        private void SetAutoAttack(ushort type)
        {
            mTypeMagic = type;
            mAutoAttack = true;
        }

        private void BreakAutoAttack()
        {
            mTypeMagic = 0;
            mAutoAttack = false;
        }

        public bool IsAutoAttack()
        {
            return mAutoAttack && mTypeMagic != 0;
        }

        #endregion

        #region Collect Targets

        private (List<Role> Roles, Point Center) CollectTargetBomb(int nLockType, int nRange)
        {
            var targets = new List<Role>();

            var center = new Point(mTargetPos.X, mTargetPos.Y);
            if (QueryMagic?.Ground == 1)
            {
                center.X = mOwner.MapX;
                center.Y = mOwner.MapY;
            }
            else if (QueryMagic?.Target == 2)
            {
                center.X = mOwner.MapX;
                center.Y = mOwner.MapY;
            }
            else if (mIdTarget != 0)
            {
                Role target = mOwner.Map.QueryAroundRole(mOwner, mIdTarget);
                if (target != null)
                {
                    center.X = target.MapX;
                    center.Y = target.MapY;
                }
            }

            List<Role> setRoles = mOwner.Map.Query9BlocksByPos(center.X, center.Y);

            foreach (Role target in setRoles)
            {
                if (target.Identity == mOwner.Identity)
                    continue;

                if (target.GetDistance(center.X, center.Y) > nRange)
                    continue;

                if (mOwner.IsImmunity(target) || !target.IsAttackable(mOwner))
                    continue;

                if (target.IsWing)
                    continue;

                targets.Add(target);
            }

            return (targets, center);
        }

        #endregion

        #region Abort Magic

        public async Task<bool> AbortMagicAsync(bool bSynchro)
        {
            BreakAutoAttack();

            if (State == MagicState.Intone) mIntone.Clear();

            State = MagicState.None;

            if (bSynchro && mOwner is Character)
                await mOwner.SendAsync(new MsgAction
                {
                    Identity = mOwner.Identity,
                    Action = MsgAction<Client>.ActionType.AbortMagic
                });

            return true;
        }

        #endregion

        #region On Timer

        public async Task OnTimerAsync()
        {
            if (!Magics.TryGetValue(mTypeMagic, out Magic magic))
            {
                State = MagicState.None;
                return;
            }

            switch (State)
            {
                case MagicState.Intone: // intone
                {
                    if (mIntone != null && !mIntone.IsTimeOut())
                        return;

                    if (mIntone != null && mIntone.IsTimeOut() && !await LaunchAsync(magic)) ResetDelay();

                    State = MagicState.None;

                    if (IsAutoAttack())
                    {
                        State = MagicState.Delay;
                        mDelay.Startup(Math.Max(MAGIC_DELAY, magic.DelayMs));
                    }

                    break;
                }

                case MagicState.Delay: // delay
                {
                    if ((mOwner.Map.IsTrainingMap() || IsAutoAttack())
                        && mDelay.IsActive()
                        && magic.Sort != MagicSort.Atkstatus)
                    {
                        if (mDelay.IsTimeOut())
                        {
                            State = MagicState.None;
                            if (!await mOwner.ProcessMagicAttackAsync(magic.Type, mIdTarget, (ushort) mTargetPos.X,
                                                                      (ushort) mTargetPos.Y))
                                State = MagicState.Delay;
                        }

                        return;
                    }

                    if (!mDelay.IsActive())
                    {
                        State = MagicState.None;
                        await AbortMagicAsync(true);
                        return;
                    }

                    if (mAutoAttack && mDelay.IsTimeOut())
                    {
                        if (mDelay.IsActive() && !mDelay.TimeOver())
                            return;

                        State = MagicState.None;
                        await mOwner.ProcessMagicAttackAsync(magic.Type, mIdTarget, (ushort) mTargetPos.X,
                                                             (ushort) mTargetPos.Y);

                        if (mIdTarget != 0 && mOwner.Map.QueryAroundRole(mOwner, mIdTarget)?.IsPlayer() == true)
                            await AbortMagicAsync(false);
                    }

                    if (mDelay.IsActive() && mDelay.TimeOver())
                    {
                        State = MagicState.None;
                        await AbortMagicAsync(false);
                    }

                    break;
                }
            }
        }

        #endregion

        public enum MagicState
        {
            None = 0,
            Intone = 1,
            Delay = 2
        }
    }
}