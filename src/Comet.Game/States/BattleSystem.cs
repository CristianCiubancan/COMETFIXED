using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.World.Maps.Enums;
using Comet.Database.Entities;
using Comet.Game.Packets;
using Comet.Game.States.Events;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.States
{
    public sealed class BattleSystem
    {
        private readonly Role mOwner;
        private readonly TimeOutMS mAttackMs = new();

        private uint mIdTarget;

        public BattleSystem(Role role)
        {
            mOwner = role;
        }

        public void CreateBattle(uint target)
        {
            mIdTarget = target;
        }

        public bool IsBattleMaintain()
        {
            if (mIdTarget == 0 || mOwner.Map == null)
                return false;

            Role target = mOwner.Map.QueryRole(mIdTarget);

            if (target == null) return false;

            if (!target.IsAlive) return false;

            if (target.MapIdentity != mOwner.MapIdentity)
                return false;

            if (mOwner is Character && target is Character && mOwner.Map?.IsPkDisable() != false)
                return false;

            if (target.IsWing && !mOwner.IsWing && !mOwner.IsBowman)
                return false;

            if (mOwner.QueryStatus(StatusSet.FATAL_STRIKE) != null)
            {
                if (mOwner.GetDistance(target) > Screen.VIEW_SIZE)
                    return false;
            }
            else
            {
                if (mOwner.GetDistance(target) > mOwner.GetAttackRange(target.SizeAddition))
                    return false;
            }

            if (!target.IsAttackable(mOwner))
                return false;

            if (mOwner.Map.IsLineSkillMap())
                return false;

            if (mOwner.Map.QueryRegion(RegionTypes.PkProtected, target.MapX, target.MapY))
                return false;

            if (mOwner is Character atkUser && atkUser.CurrentEvent != null &&
                !atkUser.CurrentEvent.IsAttackEnable(atkUser)) return false;
            return true;
        }

        public async Task<bool> ProcessAttackAsync()
        {
            if (mOwner == null || !IsBattleMaintain())
            {
                ResetBattle();
                return false;
            }

            await mOwner.MagicData.AbortMagicAsync(true);

            Role target = mOwner.Map.QueryRole(mIdTarget);
            if (target == null)
            {
                ResetBattle();
                return false;
            }

            if (mOwner.IsImmunity(target))
            {
                ResetBattle();
                return false;
            }

            var user = mOwner as Character;
            if (user?.IsBowman == true
                && !user.Map.IsTrainingMap()
                && !await user.SpendEquipItemAsync(0050, 1, true))
            {
                ResetBattle();
                return false;
            }

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnAttackAsync(user);

            if (user != null && await user.AutoSkillAttackAsync(target))
                return true;

            if (await IsTargetDodgedAsync(mOwner, target))
            {
                await mOwner.SendDamageMsgAsync(mIdTarget, 0);
                return false;
            }

            if (user != null && !user.Map.IsTrainingMap()) 
                await user.DecEquipmentDurabilityAsync(false, 0, 1);

            if (await target.CheckScapegoatAsync(mOwner))
                return false;

            var adjustAtk = 0;
            if (mOwner.QueryStatus(StatusSet.FATAL_STRIKE) != null)
                adjustAtk = mOwner.QueryStatus(StatusSet.FATAL_STRIKE).Power;

            (int Damage, InteractionEffect effect) result =
                await CalcPowerAsync(MagicType.None, mOwner, target, adjustAtk);
            InteractionEffect effect = result.effect;
            int damage = result.Damage;

            if (user?.IsLucky == true && await Kernel.ChanceCalcAsync(1, 200))
            {
                await user.SendEffectAsync("LuckyGuy", true);
                damage *= 2;
            }

            if (mOwner.QueryStatus(StatusSet.FATAL_STRIKE) != null
                && target is Monster targetMob)
            {
                if (!targetMob.IsGuard()
                    && await mOwner.JumpPosAsync(target.MapX, target.MapY))
                {
                    var msg = new MsgAction
                    {
                        Identity = mOwner.Identity,
                        Action = MsgAction.ActionType.NinjaStep,
                        Data = target.Identity,
                        CommandX = target.MapX,
                        CommandY = target.MapY,
                        X = target.MapX,
                        Y = target.MapY,
                        Timestamp = (uint) Environment.TickCount
                    };
                    await mOwner.SendAsync(msg);
                    await mOwner.Screen.UpdateAsync(msg);
                    ResetBattle();
                }
            }
            else targetMob = null;

            var lifeLost = (int) Math.Min(target.MaxLife, Math.Max(1, damage));
            long nExp = Math.Min(Math.Max(0, lifeLost), target.MaxLife);

            await mOwner.SendDamageMsgAsync(target.Identity, damage);

            await mOwner.ProcessOnAttackAsync();

            if (damage == 0)
                return true;

            await target.BeAttackAsync(MagicType.None, mOwner, damage, true);

            if (user?.CurrentEvent != null)
                await user.CurrentEvent.OnHitAsync(mOwner, target);

            if (user != null)
                await user.CheckCrimeAsync(target);

            if (targetMob != null
                && mOwner.QueryStatus(StatusSet.FATAL_STRIKE) != null
                && !targetMob.IsGuard())
            {
                await mOwner.JumpPosAsync(target.MapX, target.MapY);

                var ninjaStepMsg = new MsgAction
                {
                    Identity = mOwner.Identity,
                    Action = MsgAction<Client>.ActionType.NinjaStep,
                    Data = target.Identity,
                    X = target.MapX,
                    Y = target.MapY
                };

                if (user != null)
                {
                    await user.SendAsync(ninjaStepMsg);
                    await user.Screen.UpdateAsync(ninjaStepMsg);
                }
                else
                {
                    await mOwner.Map.BroadcastRoomMsgAsync(mOwner.MapX, mOwner.MapY, ninjaStepMsg);
                }
            }

            var npc = target as DynamicNpc;
            if (npc?.IsAwardScore() == true && user != null) await user.AddSynWarScoreAsync(npc, lifeLost);

            if (user != null &&
                (target is Monster monster && !monster.IsGuard() && !monster.IsPkKiller() && !monster.IsRighteous() ||
                 npc?.IsGoal() == true))
            {
                int nWeaponExp = (int) nExp / 3; //(int) (nExp / 10);
                nExp = user.AdjustExperience(target, nExp, false);
                var nAdditionExp = 0;
                if (!target.IsAlive && npc?.IsGoal() != true)
                {
                    nAdditionExp = (int) (target.MaxLife * 0.05f);
                    nExp += nAdditionExp;

                    if (user.Team != null)
                        await user.Team.AwardMemberExpAsync(user.Identity, target, nAdditionExp);
                }

                await user.AwardBattleExpAsync(nExp, true);

                if (!target.IsAlive && nAdditionExp > 0
                                    && !mOwner.Map.IsTrainingMap())
                    await user.SendAsync(string.Format(Language.StrKillingExperience, nAdditionExp));

                if (user.UserPackage[Item.ItemPosition.RightHand]?.IsBow() == true ||
                    user.UserPackage[Item.ItemPosition.RightHand]?.IsWeaponTwoHand() == true)
                    nWeaponExp *= 2;

                if (user.UserPackage[Item.ItemPosition.RightHand] != null)
                    await user.AddWeaponSkillExpAsync(
                        (ushort) user.UserPackage[Item.ItemPosition.RightHand].GetItemSubType(),
                        nWeaponExp);
                if (user.UserPackage[Item.ItemPosition.LeftHand] != null &&
                    !user.UserPackage[Item.ItemPosition.LeftHand].IsArrowSort())
                    await user.AddWeaponSkillExpAsync(
                        (ushort) user.UserPackage[Item.ItemPosition.LeftHand].GetItemSubType(),
                        nWeaponExp / 2);

                if (await Kernel.ChanceCalcAsync(7f))
                    await user.SendGemEffectAsync();
            }

            if (!target.IsAlive)
            {
                uint dieWay = 1;
                if (damage > target.MaxLife / 3)
                    dieWay = 2;

                await mOwner.KillAsync(target, dieWay);
            }

            return true;
        }

        public async Task OtherMemberAwardExpAsync(Role target, long nBonusExp)
        {
            if (mOwner.Map.IsTrainingMap())
                return;

            if (mOwner is Character user && user.Team != null)
                await user.Team.AwardMemberExpAsync(mOwner.Identity, target, nBonusExp);
        }

        public async Task<(int Damage, InteractionEffect effect)> CalcPowerAsync(
            MagicType magic, Role attacker, Role target, int adjustAtk = 0)
        {
            (int, InteractionEffect None) result;
            if (magic == MagicType.None)
                result = await CalcAttackPowerAsync(attacker, target, adjustAtk);
            else
                result = await CalcMagicAttackPowerAsync(attacker, target, adjustAtk);

            GameEvent @event = EventManager.GetEvent(attacker.MapIdentity);
            if (@event != null)
            {
                result.Item1 = await @event.GetDamageLimitAsync(attacker, target, result.Item1);
                return result;
            }

            if (target is DynamicNpc dynamicNpc
                && dynamicNpc.IsSynFlag()
                && dynamicNpc.IsSynMoneyEmpty())
                result.Item1 *= Role.SYNWAR_NOMONEY_DAMAGETIMES;

            return result;
        }

        public async Task<(int Damage, InteractionEffect effect)> CalcAttackPowerAsync(
            Role attacker, Role target, int adjustAtk = 0, int adjustDef = 0)
        {
            var effect = InteractionEffect.None;
            var attack = 0;
            var damage = 0;

            if (await Kernel.ChanceCalcAsync(50))
                attack = attacker.MaxAttack -
                         await Kernel.NextAsync(1, Math.Max(1, attacker.MaxAttack - attacker.MinAttack) / 2 + 1);
            else
                attack = attacker.MinAttack -
                         await Kernel.NextAsync(1, Math.Max(1, attacker.MaxAttack - attacker.MinAttack) / 2 + 1);

            if (adjustAtk > 0 && adjustAtk < Calculations.ADJUST_PERCENT)
                attack = Calculations.CutTrail(0, Calculations.AdjustDataEx(attack, adjustAtk));

            if (attacker is Character && target is Character && attacker.IsBowman)
            {
                attack = (int)(attack * (1d - (Math.Min(100, target.Dodge / 2) / 100d)));
                attack = (int) (attack * 0.1125d);
            }
            
            var targetUser = target as Character;
            int defense = 0;
            if (!attacker.IsBowman)
            {
                defense = target.Defense;

                if (adjustDef > 0)
                    defense = Calculations.CutTrail(0, Calculations.AdjustDataEx(defense, adjustDef));

                if (targetUser != null)
                    if (targetUser.Metempsychosis > 0 && targetUser.Level >= 70)
                        defense = (int) (defense * 1.3d);

                if (target.QueryStatus(StatusSet.SHIELD) != null)
                    defense = Calculations.AdjustData(defense, target.QueryStatus(StatusSet.SHIELD).Power);
            }

            damage = Math.Max(1, attack - defense);

            if (adjustAtk > Calculations.ADJUST_PERCENT)
                damage = Calculations.CutTrail(0, Calculations.AdjustDataEx(damage, adjustAtk));

            if (attacker.QueryStatus(StatusSet.STIGMA) != null)
                damage = Calculations.AdjustData(damage, attacker.QueryStatus(StatusSet.STIGMA).Power);

            if (attacker.QueryStatus(StatusSet.INTENSIFY) != null)
                damage = Calculations.AdjustData(damage, attacker.QueryStatus(StatusSet.INTENSIFY).Power);

            if (attacker.QueryStatus(StatusSet.SUPERMAN) != null && !target.IsDynaNpc() && !target.IsPlayer())
                damage = Calculations.AdjustData(damage, attacker.QueryStatus(StatusSet.SUPERMAN).Power);

            if (targetUser != null)
            {
                damage = (int) (damage * (1 - targetUser.Blessing / 100d));
                damage = (int) (damage * (1 - targetUser.TortoiseGemBonus / 100d));
            }

            if (attacker is Character atkUsr && target is Monster tgtMonster)
            {
                if (!tgtMonster.IsEquality())
                    damage = CalcDamageUser2Monster(atkUsr, target, damage);

                damage = target.AdjustWeaponDamage(damage);
                damage = AdjustMinDamageUser2Monster(damage, attacker, target);
            }
            else if (attacker is Monster atkMonster && target is Character tgtUsr)
            {
                if (!atkMonster.IsEquality())
                    damage = CalcDamageMonster2User(attacker, tgtUsr, damage);

                damage = target.AdjustWeaponDamage(damage);
                damage = AdjustMinDamageMonster2User(damage, attacker, target);
            }
            else
            {
                if (attacker is Character characterSnd && target is Character characterTgt)
                    damage = CalcDamageUser2User(characterSnd, characterTgt, damage);

                damage = target.AdjustWeaponDamage(damage);
            }

            if (attacker is Character && targetUser != null && attacker.BattlePower < target.BattlePower)
            {
                double delta = Math.Min(25, target.BattlePower - attacker.BattlePower) * 2 / 100f;
                damage = (int) (damage * (1 - delta));
            }

            damage += attacker.AddFinalAttack;
            damage -= target.AddFinalDefense;

            damage = Math.Max(damage, 1);
            return (damage, effect);
        }

        public async Task<(int Damage, InteractionEffect effect)> CalcMagicAttackPowerAsync(
            Role attacker, Role target, int adjustAtk = 0)
        {
            var effect = InteractionEffect.None;
            int attack = attacker.MagicAttack;

            if (adjustAtk > 0 && adjustAtk < Calculations.ADJUST_PERCENT)
                attack = Calculations.CutTrail(0, Calculations.AdjustDataEx(attack, adjustAtk));

            int defense = target.MagicDefense;

            int damage = attack - defense;

            damage = (int) (damage * (1 - Math.Min(target.MagicDefenseBonus, 90) / 100d));

            if (adjustAtk > 0 && adjustAtk > Calculations.ADJUST_PERCENT)
                damage = Calculations.CutTrail(0, Calculations.AdjustDataEx(damage, adjustAtk));

            var targetUser = target as Character;
            if (targetUser != null)
            {
                damage = (int) (damage * (1 - targetUser.Blessing / 100d));
                damage = (int) (damage * (1 - targetUser.TortoiseGemBonus / 100d));
            }

            if (attacker is Character atkUsr && target is Monster tgtMonster)
            {
                if (!tgtMonster.IsEquality())
                    damage = CalcDamageUser2Monster(atkUsr, target, damage);

                damage = target.AdjustWeaponDamage(damage);
                damage = AdjustMinDamageUser2Monster(damage, attacker, target);
            }
            else if (attacker is Monster atkMonster && target is Character tgtUsr)
            {
                if (!atkMonster.IsEquality())
                    damage = CalcDamageMonster2User(attacker, tgtUsr, damage);

                damage = target.AdjustWeaponDamage(damage);
                damage = AdjustMinDamageMonster2User(damage, attacker, target);
            }
            else
            {
                if (attacker is Character && target is Character)
                    damage = CalcDamageUser2User((Character) attacker, (Character) target, damage);

                damage = target.AdjustWeaponDamage(damage);
            }

            if (targetUser != null && attacker.BattlePower < target.BattlePower)
            {
                double delta = Math.Min(50, target.BattlePower - attacker.BattlePower) / 100f;
                damage = (int) (damage * (1 - delta));
            }

            damage += attacker.AddFinalMAttack;
            damage -= target.AddFinalMDefense;

            return (Math.Max(1, damage), effect);
        }

        public bool IsActive()
        {
            return mIdTarget != 0;
        }

        public bool NextAttack(int ms)
        {
            return mAttackMs.ToNextTime(ms);
        }

        public void ResetBattle()
        {
            mIdTarget = 0;
        }

        #region Static

        public static async Task<bool> IsTargetDodgedAsync(Role attacker, Role target)
        {
            const int MIN_HITRATE = 40;
            const int MIN_HITRATE_BOW_SHIELD = 25;
            const int MAX_HITRATE = 99;

            if (attacker == null || target == null || attacker.Identity == target.Identity)
                return true;

            if (attacker.QueryStatus(StatusSet.FATAL_STRIKE) != null &&
                target is Monster tgtMob) // && !tgtMob.IsGuard())
                return false;

            int hitRate = attacker.Accuracy;

            if (attacker is Character user && target is not Character)
                hitRate += 60;

            if (attacker.QueryStatus(StatusSet.STAR_OF_ACCURACY) != null)
                hitRate = Calculations.AdjustData(hitRate, attacker.QueryStatus(StatusSet.STAR_OF_ACCURACY).Power);

            int dodge = target.Dodge;

            if (!(target is Monster))
                dodge /= 2;

            if (target.QueryStatus(StatusSet.DODGE) != null)
                dodge = Calculations.AdjustData(dodge, target.QueryStatus(StatusSet.DODGE).Power);

            int minHitRate = MIN_HITRATE;
            if (attacker.IsBowman && target.IsShieldUser)
            {
                hitRate /= 2;
                minHitRate = MIN_HITRATE_BOW_SHIELD;
            }

            hitRate = Math.Min(MAX_HITRATE, Math.Max(minHitRate, minHitRate + hitRate - dodge));

#if DEBUG && DEBUG_HITRATE
            if (attacker is Character atkUser && atkUser.IsPm())
                await atkUser.SendAsync($"Attacker({attacker.Name}), Target({target.Name}), Hit Rate: {hitRate}, Target Dodge: {dodge}");

            if (target is Character targetUser && targetUser.IsPm())
                await targetUser.SendAsync($"Attacker({attacker.Name}), Target({target.Name}), Hit Rate: {hitRate}, Target Dodge: {dodge}");
#endif

            return !await Kernel.ChanceCalcAsync(hitRate);
        }

        public static int AdjustDrop(int nDrop, int nAtkLev, int nDefLev)
        {
            if (nAtkLev > 120)
                nAtkLev = 120;

            if (nAtkLev - nDefLev > 0)
            {
                int nDeltaLev = nAtkLev - nDefLev;
                if (1 < nAtkLev && nAtkLev <= 19)
                {
                    if (nDeltaLev < 3)
                    {
                    }
                    else if (nDeltaLev < 6)
                    {
                        nDrop /= 5;
                    }
                    else
                    {
                        nDrop /= 10;
                    }
                }
                else if (19 < nAtkLev && nAtkLev <= 49)
                {
                    if (nDeltaLev < 5)
                    {
                    }
                    else if (nDeltaLev < 10)
                    {
                        nDrop /= 5;
                    }
                    else
                    {
                        nDrop /= 10;
                    }
                }
                else if (49 < nAtkLev && nAtkLev <= 85)
                {
                    if (nDeltaLev < 4)
                    {
                    }
                    else if (nDeltaLev < 8)
                    {
                        nDrop /= 5;
                    }
                    else
                    {
                        nDrop /= 10;
                    }
                }
                else if (85 < nAtkLev && nAtkLev <= 112)
                {
                    if (nDeltaLev < 3)
                    {
                    }
                    else if (nDeltaLev < 6)
                    {
                        nDrop /= 5;
                    }
                    else
                    {
                        nDrop /= 10;
                    }
                }
                else if (112 < nAtkLev)
                {
                    if (nDeltaLev < 2)
                    {
                    }
                    else if (nDeltaLev < 4)
                    {
                        nDrop /= 5;
                    }
                    else
                    {
                        nDrop /= 10;
                    }
                }
            }

            return Calculations.CutTrail(0, nDrop);
        }

        public static int GetNameType(int nAtkLev, int nDefLev)
        {
            int nDeltaLev = nAtkLev - nDefLev;

            if (nDeltaLev >= 3)
                return NAME_GREEN;
            if (nDeltaLev >= 0)
                return NAME_WHITE;
            if (nDeltaLev >= -5)
                return NAME_RED;
            return NAME_BLACK;
        }

        public static int
            CalcDamageUser2Monster(Character attacker, Role target,
                                   int damage) //(int nAtk, int nDef, int nAtkLev, int nDefLev)
        {
            if (GetNameType(attacker.Level, target.Level) == NAME_GREEN)
            {
                int nDeltaLev = attacker.Level - target.Level;
                if (nDeltaLev >= 3
                    && nDeltaLev <= 5)
                    damage = (int) (damage * 1.5);
                else if (nDeltaLev > 5
                         && nDeltaLev <= 10)
                    damage *= 2;
                else if (nDeltaLev > 10
                         && nDeltaLev <= 20)
                    damage = (int) (damage * 2.5);
                else if (nDeltaLev > 20)
                    damage *= 3;
            }

            DbDisdain disdain = RoleManager.GetDisdain(attacker.BattlePower - target.BattlePower);
            int factor = disdain.MaxAtk;

            if (attacker.IsOnXpSkill())
                factor = disdain.MaxXpAtk;

            var maxDamage = (int) (target.MaxLife * (factor / 100d));
            damage = Math.Min(damage, maxDamage);

            int extraDelta = target.BattlePower - attacker.BattlePower;
            if (extraDelta > 0)
            {
                if (extraDelta >= 10) factor = 1;
                else if (extraDelta >= 5) factor = 5;
                else factor = 10;
                damage = Calculations.MulDiv(damage, factor, 100);
            }

            return damage;
        }

        public static int
            CalcDamageMonster2User(Role attacker, Character target,
                                   int damage) //(int nAtk, int nDef, int nAtkLev, int nDefLev)
        {
            int extraDelta = target.BattlePower - attacker.BattlePower;
            int factor;
            if (extraDelta < 5) factor = 100;
            else if (extraDelta < 10) factor = 80;
            else if (extraDelta < 15) factor = 60;
            else if (extraDelta < 20) factor = 40;
            else factor = 30;

            int adjustDamage = Calculations.MulDiv((int) target.MaxLife, factor * attacker.ExtraDamage, 1000000);
            return Math.Max(adjustDamage, damage);
        }

        public static int CalcDamageUser2User(Character attacker, Character target, int damage)
        {
            DbDisdain disdain = RoleManager.GetDisdain(attacker.BattlePower - target.BattlePower);

            int min, max, overAdjust;

            if (attacker.Level < 110)
            {
                if (target.Level < 110)
                {
                    min = disdain.UsrAtkUsrMin;
                    max = disdain.UsrAtkUsrMax;
                    overAdjust = disdain.UsrAtkUsrOveradj;
                }
                else
                {
                    min = disdain.UsrAtkUsrxMin;
                    max = disdain.UsrAtkUsrxMax;
                    overAdjust = disdain.UsrAtkUsrxOveradj;
                }
            }
            else
            {
                if (target.Level < 110)
                {
                    min = disdain.UsrxAtkUsrMin;
                    max = disdain.UsrxAtkUsrMax;
                    overAdjust = disdain.UsrxAtkUsrOveradj;
                }
                else
                {
                    min = disdain.UsrxAtkUsrxMin;
                    max = disdain.UsrxAtkUsrMax;
                    overAdjust = disdain.UsrxAtkUsrxOveradj;
                }
            }

            int factor = UserAttackUserGetFactor(target);
            int targetLev = target.Level;

            int minDamage = min * targetLev * factor / 100;
            if (damage < minDamage)
                return minDamage;

            int maxDamage = max * targetLev * factor / 100;
            if (damage > maxDamage)
            {
                int nDamage = Calculations.MulDiv(damage - maxDamage, overAdjust, 100);
                return nDamage + maxDamage;
            }

            return damage;
        }

        private static readonly int[] _nonRebornInts = {10, 6, 6, 6, 6};
        private static readonly int[] _rebornInts = {18, 18, 14, 27, 18};

        public static int UserAttackUserGetFactor(Character target)
        {
            int index;
            if (target.ProfessionSort == 1) index = 0;
            else if (target.ProfessionSort == 2) index = 1;
            else if (target.ProfessionSort == 4) index = 2;
            else if (target.ProfessionSort == 10 || target.ProfessionSort == 14) index = 3;
            else if (target.ProfessionSort == 13) index = 4;
            else index = 1;

            if (target.Metempsychosis > 0)
                return _rebornInts[index];
            return _nonRebornInts[index];
        }

        public static int AdjustMinDamageUser2Monster(int nDamage, Role pAtker, Role pTarget)
        {
            var nMinDamage = 1;
            nMinDamage += pAtker.Level / 10;

            if (pAtker is not Character)
                return Calculations.CutTrail(nMinDamage, nDamage);

            var pUser = (Character) pAtker;
            Item pItem = pUser.UserPackage[Item.ItemPosition.RightHand];
            if (pItem != null)
                nMinDamage += pItem.GetQuality();

            return Calculations.CutTrail(nMinDamage, nDamage);
        }

        public static int AdjustMinDamageMonster2User(int nDamage, Role pAtker, Role pTarget)
        {
            var nMinDamage = 1;
            if (nDamage >= nMinDamage
                || pTarget.Level <= 15)
                return nDamage;

            if (pTarget is not Character pUser)
                return Calculations.CutTrail(nMinDamage, nDamage);

            for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
            {
                Item item = pUser.UserPackage[pos];
                if (item == null)
                    continue;
                switch (item.Position)
                {
                    case Item.ItemPosition.Necklace:
                    case Item.ItemPosition.Headwear:
                    case Item.ItemPosition.Armor:
                        nMinDamage -= item.GetQuality() / 4;
                        break;
                }
            }

            nMinDamage = Calculations.CutTrail(1, nMinDamage);
            return Calculations.CutTrail(nMinDamage, nDamage);
        }

        public static long AdjustExp(long nDamage, int nAtkLev, int nDefLev)
        {
            if (nAtkLev > 120)
                nAtkLev = 120;

            long nExp = nDamage;

            int nNameType = NAME_WHITE;
            int nDeltaLev = nAtkLev - nDefLev;

            if (nDeltaLev >= 3)
                nNameType = NAME_GREEN;
            else if (nDeltaLev >= 0)
                nNameType = NAME_WHITE;
            else if (nDeltaLev >= -5)
                nNameType = NAME_RED;
            else nNameType = NAME_BLACK;

            if (nNameType == NAME_GREEN)
            {
                if (nDeltaLev >= 3 && nDeltaLev <= 5)
                    nExp = nExp * 70 / 100;
                else if (nDeltaLev > 5
                         && nDeltaLev <= 10)
                    nExp = nExp * 20 / 100;
                else if (nDeltaLev > 10
                         && nDeltaLev <= 20)
                    nExp = nExp * 10 / 100;
                else if (nDeltaLev > 20)
                    nExp = nExp * 5 / 100;
            }
            else if (nNameType == NAME_RED)
            {
                nExp = (int) (nExp * 1.3f);
            }
            else if (nNameType == NAME_BLACK)
            {
                if (nDeltaLev >= -10
                    && nDeltaLev < -5)
                    nExp = (int) (nExp * 1.5f);
                else if (nDeltaLev >= -20
                         && nDeltaLev < -10)
                    nExp = (int) (nExp * 1.8f);
                else if (nDeltaLev < -20)
                    nExp = (int) (nExp * 2.3f);
            }

            return Calculations.CutTrail(0, nExp);
        }

        #endregion

        public const int NAME_GREEN = 0,
                         NAME_WHITE = 1,
                         NAME_RED = 2,
                         NAME_BLACK = 3;

        public enum MagicType
        {
            None = 0,
            Normal = 1,
            XpSkill = 2
        }
    }
}