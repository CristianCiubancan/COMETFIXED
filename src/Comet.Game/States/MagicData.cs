using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Shared;

namespace Comet.Game.States
{
    public sealed partial class MagicData
    {
        private readonly Role mOwner;

        public ConcurrentDictionary<uint, Magic> Magics = new();

        public MagicData(Role role)
        {
            mOwner = role;
        }

        public async Task<bool> InitializeAsync()
        {
            if (mOwner.IsPlayer())
            {
                var user = (Character) mOwner;

                foreach (DbMagic dbMagic in await MagicRepository.GetAsync(mOwner.Identity))
                {
                    var magic = new Magic(mOwner);
                    if (!await magic.CreateAsync(dbMagic))
                        continue;

                    if (magic.Type == 1100 && user != null && user.ProfessionSort != 13)
                    {
                        await ServerDbContext.DeleteAsync(dbMagic);
                        continue;
                    }

                    if (magic.Type == 1025 && user != null && user.ProfessionSort != 2 && user.ProfessionSort != 13)
                    {
                        await ServerDbContext.DeleteAsync(dbMagic);
                        continue;
                    }

                    if (magic.Type == 1050 && user != null && user.ProfessionSort != 14 && user.ProfessionSort != 13)
                    {
                        await ServerDbContext.DeleteAsync(dbMagic);
                        continue;
                    }

                    Magics.TryAdd(magic.Type, magic);
                }
            }

            return true;
        }

        #region Magic Checking

        public bool CheckType(ushort type)
        {
            return Magics.ContainsKey(type) && !Magics[type].Unlearn;
        }

        public async Task<bool> CreateAsync(ushort type, byte level)
        {
            if (this[type] != null)
            {
                Magic old = this[type];
                old.Unlearn = false;

                if (mOwner is Character)
                {
                    await old.SaveAsync();
                    await old.SendAsync();
                }

                return true;
            }

            var pMagic = new Magic(mOwner);
            if (await pMagic.CreateAsync(type, level)) return Magics.TryAdd(type, pMagic);
            return false;
        }

        public async Task<bool> UpLevelByTaskAsync(ushort type)
        {
            Magic pMagic;
            if (!Magics.TryGetValue(type, out pMagic))
                return false;

            var nNewLevel = (byte) (pMagic.Level + 1);
            if (!FindMagicType(type, nNewLevel))
                return false;

            pMagic.Experience = 0;
            pMagic.Level = nNewLevel;
            await pMagic.SendAsync();
            await pMagic.SaveAsync();
            return true;
        }

        public bool FindMagicType(ushort type, byte pLevel)
        {
            return MagicManager.GetMagictype(type, pLevel) != null;
        }

        public bool CheckLevel(ushort type, ushort level)
        {
            return Magics.Values.FirstOrDefault(x => x.Type == type && x.Level == level) != null;
        }

        public bool IsWeaponMagic(ushort type)
        {
            return type >= 10000 && type < 10256;
        }

        private BattleSystem.MagicType HitByMagic(Magic magic)
        {
            // 0 none, 1 normal, 2 xp
            if (magic == null) return 0;

            if (magic.WeaponHit == 0)
                return magic.UseXp == BattleSystem.MagicType.XpSkill
                           ? BattleSystem.MagicType.XpSkill
                           : BattleSystem.MagicType.Normal;

            if (mOwner is Character pRole)
                if (pRole.UserPackage[Item.ItemPosition.RightHand] != null && magic.WeaponHit == 2 &&
                    pRole.UserPackage[Item.ItemPosition.RightHand].Itemtype.MagicAtk > 0)
                    return magic.UseXp == BattleSystem.MagicType.XpSkill
                               ? BattleSystem.MagicType.XpSkill
                               : BattleSystem.MagicType.Normal;

            return BattleSystem.MagicType.None;
        }

        private uint GetDieMode()
        {
            return (uint) (HitByMagic(QueryMagic) > 0 ? 3 : mOwner.IsBowman ? 5 : 1);
        }

        public bool HitByWeapon()
        {
            Magic magic = QueryMagic;
            if (magic == null)
                return true;

            if (magic.WeaponHit == 1)
                return true;

            Item pItem;
            if (mOwner is Character character
                && (pItem = character.UserPackage[Item.ItemPosition.RightHand]) != null
                && pItem.Itemtype.MagicAtk <= 0)
                return true;

            return false;
        }

        public async Task<bool> UnlearnMagicAsync(ushort type, bool drop)
        {
            Magic magic = this[type];
            if (magic == null)
                return false;

            if (drop)
            {
                await magic.DeleteAsync();
            }
            else
            {
                magic.OldLevel = (byte) magic.Level;
                magic.Level = 0;
                magic.Experience = 0;
                magic.Unlearn = true;
                await magic.SaveAsync();
            }

            await mOwner.SendAsync(new MsgAction
            {
                Identity = mOwner.Identity,
                Command = type,
                Action = MsgAction<Client>.ActionType.SpellRemove
            });

            return Magics.TryRemove(type, out _);
        }

        public async Task<bool> ResetMagicAsync(ushort type)
        {
            Magic magic = this[type];
            if (magic == null)
                return false;

            magic.OldLevel = (byte) magic.Level;
            magic.Level = 0;
            magic.Experience = 0;
            await magic.SaveAsync();
            await magic.SendAsync();
            return true;
        }

        #endregion

        #region Experience

        public async Task<bool> AwardExpOfLifeAsync(Role pTarget, int nLifeLost, Magic magic = null,
                                                    bool bMagicRecruit = false)
        {
            if (mOwner is not Character owner)
            {
                owner = mOwner.IsCallPet() ? RoleManager.GetUser(mOwner.OwnerIdentity) : null;
            }

            if (owner != null && (pTarget.IsMonster() || pTarget is DynamicNpc dynamicNpc && dynamicNpc.IsGoal()))
            {
                int exp = nLifeLost;
                long battleExp = owner.AdjustExperience(pTarget, nLifeLost, false);

                if (!pTarget.IsAlive && !bMagicRecruit)
                {
                    var nBonusExp = (int) (pTarget.MaxLife * (5 / 100));
                    battleExp += nBonusExp;
                    if (!owner.Map.IsTrainingMap() && nBonusExp > 0)
                        await owner.SendAsync(string.Format(Language.StrKillingExperience, nBonusExp));
                }

                await AwardExpAsync(0, (int) battleExp, exp, magic);
            }

            return true;
        }

        public async Task<bool> AwardExpAsync(int nType, long nBattleExp, long nExp, Magic pMagic = null)
        {
            if (pMagic == null)
                return await AwardExpAsync(nBattleExp, nExp, true, QueryMagic);
            return await AwardExpAsync(nBattleExp, nExp, true, pMagic);
        }

        public async Task<bool> AwardExpAsync(long nBattleExp, long nExp, bool bIgnoreFlag, Magic pMagic = null)
        {
            if (nBattleExp <= 0 && nExp == 0) return false;

            pMagic ??= QueryMagic;

            if (mOwner.Map.IsTrainingMap())
                if (nBattleExp > 0)
                {
                    if (mOwner.IsBowman)
                        nBattleExp /= 2;
                    nBattleExp = Calculations.CutTrail(1, Calculations.MulDiv(nBattleExp, 10, 100));
                }

            if (nBattleExp > 0 && mOwner is Character user)
                await user.AwardBattleExpAsync(nBattleExp, true);

            if (pMagic == null)
                return false;

            if (!CheckAwardExpEnable(pMagic))
                return false;

            if (mOwner.Map.Identity == TC_PK_ARENA_ID)
                return true;

            if (mOwner.Map.IsTrainingMap() && pMagic.AutoActive == 0 && mAutoAttackNum > 0 &&
                mAutoAttackNum % 10 != 0)
                return true;

            if (pMagic.NeedExp > 0
                && (pMagic.AutoActive & 16) == 0
                || bIgnoreFlag)
            {
                if (mOwner is Character owner)
                    nExp = (int) (nExp * (1 + owner.MoonGemBonus / 100d));

                pMagic.Experience += (uint) nExp;

                //if ((pMagic.AutoActive & 8) == 0)
                await pMagic.FlushAsync();

                await UpLevelMagic(true, pMagic);
                await pMagic.SaveAsync();
                return true;
            }

            if (pMagic.NeedExp == 0
                && pMagic.Target == 4)
            {
                if (mOwner is Character owner)
                    nExp = (int) (nExp * (1 + owner.MoonGemBonus / 100d));

                pMagic.Experience += (uint) nExp;

                //if ((pMagic.AutoActive & 8) == 0)
                await pMagic.FlushAsync();
                await UpLevelMagic(true, pMagic);

                await pMagic.SaveAsync();
                return true;
            }

            return false;
        }

        public async Task<bool> UpLevelMagic(bool synchro, Magic pMagic)
        {
            if (pMagic == null)
                return false;

            int nNeedExp = pMagic.NeedExp;

            if (!(nNeedExp > 0
                  && (pMagic.Experience >= nNeedExp
                      || pMagic.OldLevel > 0
                      && pMagic.Level >= pMagic.OldLevel / 2
                      && pMagic.Level < pMagic.OldLevel)))
                return false;

            var nNewLevel = (ushort) (pMagic.Level + 1);
            pMagic.Experience = 0;
            pMagic.Level = nNewLevel;
            if (synchro)
                await pMagic.SendAsync();
            return true;
        }

        public bool CheckAwardExpEnable(Magic magic)
        {
            if (magic == null)
                return false;
            return mOwner.Level >= magic.NeedLevel
                   && magic.NeedExp > 0
                   && mOwner.MapIdentity != 1005;
        }

        #endregion

        #region Crime

        public async Task<bool> CheckCrimeAsync(Role pRole, Magic magic)
        {
            if (pRole == null || magic == null) return false;

            if (magic.Crime <= 0)
                return false;

            return await mOwner.CheckCrimeAsync(pRole);
        }

        public async Task<bool> CheckCrimeAsync(Dictionary<uint, Role> pRoleSet, Magic magic)
        {
            if (pRoleSet == null || magic == null) return false;

            if (magic.Crime <= 0)
                return false;

            foreach (Role pRole in pRoleSet.Values)
                if (mOwner.Identity != pRole.Identity && await mOwner.CheckCrimeAsync(pRole))
                    return true;
            return false;
        }

        #endregion

        #region Socket

        public async Task SendAllAsync()
        {
            foreach (Magic magic in Magics.Values.Where(x => !x.Unlearn)) await magic.SendAsync();
        }

        #endregion

        public Magic QueryMagic => Magics.TryGetValue(mTypeMagic, out Magic magic) ? magic : null;

        public Magic this[ushort nType] => Magics.TryGetValue(nType, out Magic ret) ? ret : null;

        public enum MagicSort
        {
            Attack = 1,
            Recruit = 2, // support auto active.
            Cross = 3,
            Fan = 4, // support auto active(random).
            Bomb = 5,
            Attachstatus = 6,
            Detachstatus = 7,
            Square = 8,
            Jumpattack = 9,   // move, a-lock
            Randomtrans = 10, // move, a-lock
            Dispatchxp = 11,
            Collide = 12,   // move, a-lock & b-synchro
            Serialcut = 13, // auto active only.
            Line = 14,      // support auto active(random).
            Atkrange = 15,  // auto active only, forever active.
            Atkstatus = 16, // support auto active, random active.
            Callteammember = 17,
            Recordtransspell = 18,
            Transform = 19,
            Addmana = 20, // support self target only.
            Laytrap = 21,
            Dance = 22,       // ÌøÎè(only use for client)
            Callpet = 23,     // ÕÙ»½ÊÞ
            Vampire = 24,     // ÎüÑª£¬power is percent award. use for call pet
            Instead = 25,     // ÌæÉí. use for call pet
            Declife = 26,     // ¿ÛÑª(µ±Ç°ÑªµÄ±ÈÀý)
            Groundsting = 27, // µØ´Ì,
            Vortex = 28,
            Activateswitch = 29,
            Spook = 30,
            Warcry = 31,
            Riding = 32,
            AttachstatusArea = 34,
            Remotebomb = 35, // fuck tq i dont know what name to use _|_
            Knockback = 38,
            Dashwhirl = 40,
            Perseverance = 41,
            Selfdetach = 46,
            Detachbadstatus = 47,
            CloseLine = 48,
            Compassion = 50,
            Teamflag = 51,
            Increaseblock = 52,
            Oblivion = 53,
            Stunbomb = 54,
            Tripleattack = 55,
            Dashdeadmark = 61,
            Mountwhirl = 64,
            Targetdrag = 65,
            Closescatter = 67,
            Assassinvortex = 68,
            Blisteringwave = 69
        }

        public const int PURE_TROJAN_ID = 10315;
        public const int PURE_WARRIOR_ID = 10311;
        public const int PURE_ARCHER_ID = 10313;
        public const int PURE_NINJA_ID = 6003;
        public const int PURE_MONK_ID = 10405;
        public const int PURE_PIRATE_ID = 11040;
        public const int PURE_WATER_ID = 30000;
        public const int PURE_FIRE_ID = 10310;

        public const int TWOFOLDBLADES_ID = 6000;

        public const int MAGICDAMAGE_ALT = 26;
        public const int AUTOLEVELUP_EXP = -1;
        public const int DISABLELEVELUP_EXP = 0;
        public const int AUTOMAGICLEVEL_PER_USERLEVEL = 10;
        public const int USERLEVELS_PER_MAGICLEVEL = 10;

        public const int KILLBONUS_PERCENT = 5;
        public const int HAVETUTORBONUS_PERCENT = 10;
        public const int WITHTUTORBONUS_PERCENT = 20;

        public const int MAGIC_DELAY = 1000; // DELAY
        public const int MAGIC_DECDELAY_PER_LEVEL = 100;
        public const int RANDOMTRANS_TRY_TIMES = 10;
        public const int DISPATCHXP_NUMBER = 20;
        public const int COLLIDE_POWER_PERCENT = 80;
        public const int COLLIDE_SHIELD_DURABILITY = 3;
        public const int LINE_WEAPON_DURABILITY = 2;
        public const int MAX_SERIALCUTSIZE = 10;
        public const int AWARDEXP_BY_TIMES = 1;
        public const int AUTO_MAGIC_DELAY_PERCENT = 150;
        public const int BOW_SUBTYPE = 500;
        public const ushort POISON_MAGIC_TYPE = 10010;
        public const int DEFAULT_MAGIC_FAN = 120;
        public const int STUDENTBONUS_PERCENT = 5;

        public const int MAGIC_KO_LIFE_PERCENT = 15;
        public const int MAGIC_ESCAPE_LIFE_PERCENT = 15;
    }
}