using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Core.World.Maps;
using Comet.Core.World.Maps.Enums;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Internal.AI;
using Comet.Game.Internal.Auth;
using Comet.Game.Packets;
using Comet.Game.Packets.Ai;
using Comet.Game.States.Events;
using Comet.Game.States.Families;
using Comet.Game.States.Guide;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Game.States.Relationship;
using Comet.Game.States.Syndicates;
using Comet.Game.World;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Network.Packets.Ai;
using Comet.Network.Packets.Game;
using Comet.Network.Packets.Internal;
using Comet.Shared;
using MsgAction = Comet.Game.Packets.MsgAction;
using MsgInteract = Comet.Game.Packets.MsgInteract;
using MsgTalk = Comet.Game.Packets.MsgTalk;

namespace Comet.Game.States
{
    /// <summary>
    ///     Character class defines a database record for a player's character. This allows
    ///     for easy saving of character information, as well as means for wrapping character
    ///     data for spawn packet maintenance, interface update pushes, etc.
    /// </summary>
    public class Character : Role
    {
        private readonly DbCharacter mDbObject;

        private readonly TimeOut mFlowerRankRefresh = new(1);
        private readonly TimeOutMS mEnergyTm = new(ADD_ENERGY_STAND_MS);
        private readonly TimeOut mAutoHeal = new(AUTOHEALLIFE_TIME);
        private readonly TimeOut mPkDecrease = new(PK_DEC_TIME);
        private readonly TimeOutMS mStatusTm = new(200);
        private readonly TimeOut mXpPoints = new(3);
        private readonly TimeOut mGhost = new(3);
        private TimeOut mTransformation = new();
        private readonly TimeOut mRevive = new();
        private readonly TimeOut mRespawn = new();
        private readonly TimeOut mMine = new(2);
        private readonly TimeOut mTeamLeaderPos = new(3);
        private readonly TimeOut mHeavenBlessing = new(60);
        private readonly TimeOut mLuckyAbsorbStart = new(2);
        private readonly TimeOut mLuckyStep = new(1);
        private readonly TimeOutMS mVigorTimer = new(1500);
        private readonly TimeOut mEnlightenTimeExp = new(ENLIGHTENMENT_EXP_PART_TIME);

        private int mBlessPoints;
        private uint mIdLuckyTarget;
        private int mLuckyTimeCount;
        private int mKillsToCaptcha;

        private readonly ConcurrentDictionary<RequestType, uint> mDicRequests = new();

        /// <summary>
        ///     Instantiates a new instance of <see cref="Character" /> using a database fetched
        ///     <see cref="DbCharacter" />. Copies attributes over to the base class of this
        ///     class, which will then be used to save the character from the game world.
        /// </summary>
        /// <param name="character">Database character information</param>
        /// <param name="socket"></param>
        public Character(DbCharacter character, Client socket)
        {
            /*
             * Removed the base class because we'll be inheriting role stuff.
             */
            mDbObject = character;

            if (socket == null)
                return; // ?

            Client = socket;

            m_mesh = mDbObject.Mesh;

            mPosX = character.X;
            mPosY = character.Y;
            mIdMap = character.MapID;

            Screen = new Screen(this);
            WeaponSkill = new WeaponSkill(this);
            UserPackage = new UserPackage(this);
            Statistic = new UserStatistic(this);
            TaskDetail = new TaskDetail(this);

            if (mDbObject.LuckyTime != null)
                mLuckyTimeCount = (int) Math.Max(0, (mDbObject.LuckyTime.Value - DateTime.Now).TotalSeconds);

            if (EnlightenExperience > 0)
                mEnlightenTimeExp.Startup(ENLIGHTENMENT_EXP_PART_TIME);

            mEnergyTm.Update();
            mAutoHeal.Update();
            mPkDecrease.Update();
            mXpPoints.Update();
            mGhost.Update();
            mStatusTm.Update();
        }

        public Client Client { get; }

        public MessageBox MessageBox { get; set; }
        public bool IsConnected => Client.Socket.Connected;

        #region Identity

        public override uint Identity
        {
            get => mDbObject.Identity;
            protected set
            {
                // cannot change the identity
            }
        }

        public override string Name
        {
            get => mDbObject.Name;
            set => mDbObject.Name = value;
        }

        public string MateName { get; set; }

        public uint MateIdentity
        {
            get => mDbObject.Mate;
            set => mDbObject.Mate = value;
        }

        public TimeSpan OnlineTime => TimeSpan.Zero
                                              .Add(new TimeSpan(0, 0, 0, mDbObject.OnlineSeconds))
                                              .Add(new TimeSpan(
                                                       0, 0, 0,
                                                       (int) (DateTime.Now - mDbObject.LoginTime).TotalSeconds));

        public TimeSpan SessionOnlineTime => TimeSpan.Zero
                                                     .Add(new TimeSpan(
                                                              0, 0, 0,
                                                              (int) (DateTime.Now - mDbObject.LoginTime)
                                                              .TotalSeconds));

        #endregion

        #region Administration

        public bool IsPm()
        {
            return Name.Contains("[PM]");
        }

        public bool IsGm()
        {
            return IsPm() || Name.Contains("[GM]");
        }

        public bool ShowAction { get; set; } = false;

        #endregion

        #region Appearence

        private uint m_mesh;
        private ushort m_transformMesh;

        public int Gender => Body == BodyType.AgileMale || Body == BodyType.MuscularMale ? 1 : 2;

        public ushort TransformationMesh
        {
            get => m_transformMesh;
            set
            {
                m_transformMesh = value;
                Mesh = (uint) ((uint) value * 10000000 + Avatar * 10000 + (uint) Body);
            }
        }

        public override uint Mesh
        {
            get => m_mesh;
            set
            {
                m_mesh = value;
                mDbObject.Mesh = value % 10000000;
            }
        }

        public BodyType Body
        {
            get => (BodyType) (Mesh % 10000);
            set => Mesh = (uint) value + Avatar * 10000u;
        }

        public ushort Avatar
        {
            get => (ushort) (Mesh % 10000000 / 10000);
            set => Mesh = (uint) (value * 10000 + (int) Body);
        }

        public ushort Hairstyle
        {
            get => mDbObject.Hairstyle;
            set => mDbObject.Hairstyle = value;
        }

        #endregion

        #region Transformation

        public Transformation Transformation { get; protected set; }

        public async Task<bool> TransformAsync(uint dwLook, int nKeepSecs, bool bSynchro)
        {
            var bBack = false;

            if (Transformation != null)
            {
                await ClearTransformationAsync();
                bBack = true;
            }

            DbMonstertype pType = RoleManager.GetMonstertype(dwLook);
            if (pType == null) return false;

            var pTransform = new Transformation(this);
            if (pTransform.Create(pType))
            {
                Transformation = pTransform;
                TransformationMesh = (ushort) pTransform.Lookface;
                await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);
                Life = MaxLife;
                mTransformation = new TimeOut(nKeepSecs);
                mTransformation.Startup(nKeepSecs);
                if (bSynchro)
                    await SynchroTransformAsync();
            }
            else
            {
                pTransform = null;
            }

            if (bBack)
                await SynchroTransformAsync();

            return false;
        }

        public async Task ClearTransformationAsync()
        {
            TransformationMesh = 0;
            Transformation = null;
            mTransformation.Clear();

            await SynchroTransformAsync();
            await MagicData.AbortMagicAsync(true);
            BattleSystem.ResetBattle();
        }

        public async Task<bool> SynchroTransformAsync()
        {
            var msg = new MsgUserAttrib(Identity, ClientUpdateType.Mesh, Mesh);
            if (TransformationMesh != 98 && TransformationMesh != 99)
            {
                Life = MaxLife;
                msg.Append(ClientUpdateType.MaxHitpoints, MaxLife);
                msg.Append(ClientUpdateType.Hitpoints, Life);
            }

            await BroadcastRoomMsgAsync(msg, true);
            return true;
        }

        public async Task SetGhostAsync()
        {
            if (IsAlive) return;

            ushort trans = 98;
            if (Gender == 2)
                trans = 99;
            TransformationMesh = trans;
            await SynchroTransformAsync();
        }

        #endregion

        #region Profession

        public byte ProfessionSort => (byte) (Profession / 10);

        public byte ProfessionLevel => (byte) (Profession % 10);

        public byte Profession
        {
            get => mDbObject?.Profession ?? 0;
            set => mDbObject.Profession = value;
        }

        public byte PreviousProfession
        {
            get => mDbObject?.PreviousProfession ?? 0;
            set => mDbObject.PreviousProfession = value;
        }

        public byte FirstProfession
        {
            get => mDbObject?.FirstProfession ?? 0;
            set => mDbObject.FirstProfession = value;
        }

        #endregion

        #region Attribute Points

        public ushort Strength
        {
            get => mDbObject?.Strength ?? 0;
            set => mDbObject.Strength = value;
        }

        public ushort Agility
        {
            get => mDbObject?.Agility ?? 0;
            set => mDbObject.Agility = value;
        }

        public ushort Vitality
        {
            get => mDbObject?.Vitality ?? 0;
            set => mDbObject.Vitality = value;
        }

        public ushort Spirit
        {
            get => mDbObject?.Spirit ?? 0;
            set => mDbObject.Spirit = value;
        }

        public ushort AttributePoints
        {
            get => mDbObject?.AttributePoints ?? 0;
            set => mDbObject.AttributePoints = value;
        }

        #endregion

        #region Battle Attributes

        public override int BattlePower
        {
            get
            {
                int result = Level + Metempsychosis * 5 + (int) NobilityRank;
                if (SyndicateIdentity > 0)
                    result += Syndicate.GetSharedBattlePower(SyndicateRank);
                result += Math.Max(FamilyBattlePower, Guide?.SharedBattlePower ?? 0);
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.BattlePower ?? 0;
                return result;
            }
        }

        public int PureBattlePower
        {
            get
            {
                int result = Level + Metempsychosis * 5 + (int) NobilityRank;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.BattlePower ?? 0;
                return result;
            }
        }

        public override int MinAttack
        {
            get
            {
                if (Transformation != null)
                    return Transformation.MinAttack;

                int result = Strength;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    if (pos == Item.ItemPosition.LeftHand)
                        result += (UserPackage[pos]?.MinAttack ?? 0) / 2;
                    else
                        result += UserPackage[pos]?.MinAttack ?? 0;
                }

                result = (int) (result * (1 + DragonGemBonus / 100d));
                return result;
            }
        }

        public override int MaxAttack
        {
            get
            {
                if (Transformation != null)
                    return Transformation.MaxAttack;

                int result = Strength;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    if (pos == Item.ItemPosition.LeftHand)
                        result += (UserPackage[pos]?.MaxAttack ?? 0) / 2;
                    else
                        result += UserPackage[pos]?.MaxAttack ?? 0;
                }

                result = (int) (result * (1 + DragonGemBonus / 100d));
                return result;
            }
        }

        public override int MagicAttack
        {
            get
            {
                if (Transformation != null)
                    return Transformation.MaxAttack;

                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    result += UserPackage[pos]?.MagicAttack ?? 0;
                }

                result = (int) (result * (1 + PhoenixGemBonus / 100d));
                return result;
            }
        }

        public override int Defense
        {
            get
            {
                if (Transformation != null)
                    return Transformation.Defense;
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    result += UserPackage[pos]?.Defense ?? 0;
                }

                return result;
            }
        }

        public override int Defense2
        {
            get
            {
                if (Transformation != null)
                    return (int) Transformation.Defense2;
                return QueryStatus(StatusSet.VORTEX) != null       ? 1 :
                       Metempsychosis >= 1 && ProfessionLevel >= 3 ? 7000 : Calculations.DEFAULT_DEFENCE2;
            }
        }

        public override int MagicDefense
        {
            get
            {
                if (Transformation != null)
                    return Transformation.MagicDefense;
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos == Item.ItemPosition.AttackTalisman || pos == Item.ItemPosition.DefenceTalisman)
                        continue;

                    result += UserPackage[pos]?.MagicDefense ?? 0;
                }

                return result;
            }
        }

        public override int MagicDefenseBonus
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.MagicDefenseBonus ?? 0;
                return result;
            }
        }

        public override int Dodge
        {
            get
            {
                if (Transformation != null)
                    return (int) Transformation.Dodge;
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    if (pos != Item.ItemPosition.Steed || QueryStatus(StatusSet.RIDING) != null)
                        result += UserPackage[pos]?.Dodge ?? 0;
                }
                return result;
            }
        }

        public override int Blessing
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.Blessing ?? 0;
                return result;
            }
        }

        public override int AddFinalAttack
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin;
                     pos <= Item.ItemPosition.EquipmentEnd;
                     pos++)
                    result += UserPackage[pos]?.AddFinalDamage ?? 0;

                return result;
            }
        }

        public override int AddFinalMAttack
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin;
                     pos <= Item.ItemPosition.EquipmentEnd;
                     pos++)
                    result += UserPackage[pos]?.AddFinalMagicDamage ?? 0;

                return result;
            }
        }

        public override int AddFinalDefense
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin;
                     pos <= Item.ItemPosition.EquipmentEnd;
                     pos++)
                    result += UserPackage[pos]?.AddFinalDefense ?? 0;

                return result;
            }
        }

        public override int AddFinalMDefense
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.AddFinalMagicDefense ?? 0;
                return result;
            }
        }

        public override int AttackSpeed { get; } = 1000;

        public override int Accuracy
        {
            get
            {
                int result = Agility;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.Accuracy ?? 0;
                return result;
            }
        }

        public int DragonGemBonus
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                {
                    Item item = UserPackage[pos];
                    if (item != null) result += item.DragonGemEffect;
                }

                return result;
            }
        }

        public int PhoenixGemBonus
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.PhoenixGemEffect ?? 0;
                return result;
            }
        }

        public int VioletGemBonus
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.VioletGemEffect ?? 0;
                return result;
            }
        }

        public int MoonGemBonus
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.MoonGemEffect ?? 0;
                return result;
            }
        }

        public int RainbowGemBonus
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.RainbowGemEffect ?? 0;
                return result;
            }
        }

        public int FuryGemBonus
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.FuryGemEffect ?? 0;
                return result;
            }
        }

        public int TortoiseGemBonus
        {
            get
            {
                var result = 0;
                for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                    result += UserPackage[pos]?.TortoiseGemEffect ?? 0;
                return result;
            }
        }

        public int KoCount { get; set; }

        #endregion

        #region Level and Experience

        public bool AutoAllot
        {
            get => mDbObject.AutoAllot != 0;
            set => mDbObject.AutoAllot = (byte) (value ? 1 : 0);
        }

        public override byte Level
        {
            get => mDbObject?.Level ?? 0;
            set => mDbObject.Level = Math.Min(MAX_UPLEV, Math.Max((byte) 1, value));
        }

        public ulong Experience
        {
            get => mDbObject?.Experience ?? 0;
            set
            {
                if (Level >= MAX_UPLEV)
                    return;

                mDbObject.Experience = value;
            }
        }

        public byte Metempsychosis
        {
            get => mDbObject?.Rebirths ?? 0;
            set => mDbObject.Rebirths = value;
        }

        public bool IsNewbie()
        {
            return Level < 70;
        }

        public async Task<bool> AwardLevelAsync(ushort amount)
        {
            if (Level >= MAX_UPLEV)
                return false;

            if (Level + amount <= 0)
                return false;

            int addLev = amount;
            if (addLev + Level > MAX_UPLEV)
                addLev = MAX_UPLEV - Level;

            if (addLev <= 0)
                return false;

            await AddAttributesAsync(ClientUpdateType.Atributes, (ushort) (addLev * 3));
            await AddAttributesAsync(ClientUpdateType.Level, addLev);
            await BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = Identity,
                Action = MsgAction<Client>.ActionType.CharacterLevelUp,
                ArgumentX = MapX,
                ArgumentY = MapY
            }, true);

            await UpLevelEventAsync();
            return true;
        }

        public async Task AwardBattleExpAsync(long nExp, bool bGemEffect)
        {
            if (nExp == 0 || QueryStatus(StatusSet.CURSED) != null)
                return;

            if (Level >= MAX_UPLEV)
                return;

            if (nExp < 0)
            {
                await AddAttributesAsync(ClientUpdateType.Experience, nExp);
                return;
            }

            const int battleExpTax = 5;

            if (Level >= 120)
                nExp /= 2;

            if (Level < 130)
                nExp *= battleExpTax;

            double multiplier = 1;
            if (HasMultipleExp)
                multiplier += ExperienceMultiplier - 1;

            if (!IsNewbie() && ProfessionSort == 13 && ProfessionLevel >= 3)
                multiplier += 1;

            DbLevelExperience levExp = ExperienceManager.GetLevelExperience(Level);
            if (IsBlessed)
                if (levExp != null)
                    OnlineTrainingExp += (uint) (levExp.UpLevTime * (nExp / (float) levExp.Exp) * 0.2);

            if (Guide != null && levExp != null)
                await Guide.AwardTutorExperienceAsync((uint) (levExp.MentorUpLevTime * ((float) nExp / levExp.Exp)));

            if (bGemEffect)
                multiplier += 1 + RainbowGemBonus / 100d;

            if (IsLucky && await Kernel.ChanceCalcAsync(10, 10000))
            {
                await SendEffectAsync("LuckyGuy", true);
                nExp *= 5;
                await SendAsync(Language.StrLuckyGuyQuintuple);
            }

            multiplier += 1 + BattlePower / 100d;

            nExp = (long) (nExp * Math.Max(0.01d, multiplier));

            if (Metempsychosis >= 2)
                nExp /= 3;

            await AwardExperienceAsync(nExp);
        }

        public long AdjustExperience(Role pTarget, long nRawExp, bool bNewbieBonusMsg)
        {
            if (pTarget == null) return 0;
            long nExp = nRawExp;
            nExp = BattleSystem.AdjustExp(nExp, Level, pTarget.Level);
            return nExp;
        }

        public async Task<bool> AwardExperienceAsync(long amount, bool noContribute = false)
        {
            if (Level > ExperienceManager.GetLevelLimit())
                return true;

            amount += (long) Experience;
            var leveled = false;
            uint pointAmount = 0;
            byte newLevel = Level;
            ushort virtue = 0;
            long usedExp = amount;

            double mentorUpLevTime = 0;
            while (newLevel < MAX_UPLEV && amount >= (long) ExperienceManager.GetLevelExperience(newLevel).Exp)
            {
                DbLevelExperience dbExp = ExperienceManager.GetLevelExperience(newLevel);
                amount -= (long) dbExp.Exp;
                leveled = true;
                newLevel++;

                if (newLevel <= 70) virtue += (ushort) dbExp.UpLevTime;

                if (!AutoAllot || newLevel >= 120)
                    pointAmount += 3;

                mentorUpLevTime += dbExp.MentorUpLevTime;

                if (newLevel < ExperienceManager.GetLevelLimit()) continue;
                amount = 0;
                break;
            }

            uint metLev = 0;
            DbLevelExperience leveXp = ExperienceManager.GetLevelExperience(newLevel);
            if (leveXp != null)
            {
                float fExp = amount / (float) leveXp.Exp;
                metLev = (uint) (newLevel * 10000 + fExp * 1000);

                mentorUpLevTime += leveXp.MentorUpLevTime * ((float) amount / leveXp.Exp);
            }

            byte checkLevel = 130; //(byte)(m_dbObject.Reincarnation > 0 ? 110 : 130);
            if (newLevel >= checkLevel && Metempsychosis > 0 && mDbObject.MeteLevel > metLev)
            {
                byte extra = 0;
                if (mDbObject.MeteLevel / 10000 > newLevel)
                {
                    uint mete = mDbObject.MeteLevel / 10000;
                    extra += (byte) (mete - newLevel);
                    pointAmount += (uint) (extra * 3);
                    leveled = true;
                    amount = 0;
                }

                newLevel += extra;

                if (newLevel >= ExperienceManager.GetLevelLimit())
                {
                    newLevel = (byte) ExperienceManager.GetLevelLimit();
                    amount = 0;
                }
                else if (mDbObject.MeteLevel >= newLevel * 10000)
                {
                    amount = (long) (ExperienceManager.GetLevelExperience(newLevel).Exp *
                                     (mDbObject.MeteLevel % 10000 / 1000d));
                }
            }

            if (leveled)
            {
                byte job;
                if (Profession > 100)
                    job = 10;
                else
                    job = (byte) ((Profession - Profession % 10) / 10);

                DbPointAllot allot = ExperienceManager.GetPointAllot(job, Math.Min((byte) 120, newLevel));
                Level = newLevel;
                if (AutoAllot && allot != null)
                {
                    await SetAttributesAsync(ClientUpdateType.Strength, allot.Strength);
                    await SetAttributesAsync(ClientUpdateType.Agility, allot.Agility);
                    await SetAttributesAsync(ClientUpdateType.Vitality, allot.Vitality);
                    await SetAttributesAsync(ClientUpdateType.Spirit, allot.Spirit);
                }
                
                if (pointAmount > 0)
                {
                    await AddAttributesAsync(ClientUpdateType.Atributes, (int) pointAmount);
                }

                await SetAttributesAsync(ClientUpdateType.Level, Level);
                await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
                await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
                await Screen.BroadcastRoomMsgAsync(new MsgAction
                {
                    Action = MsgAction<Client>.ActionType.CharacterLevelUp,
                    Identity = Identity
                });

                await UpLevelEventAsync();

                if (!noContribute && Guide != null && mentorUpLevTime > 0)
                    await Guide.AwardTutorExperienceAsync((uint) mentorUpLevTime).ConfigureAwait(false);
            }

            if (Team != null && !Team.IsLeader(Identity) && virtue > 0)
            {
                Team.Leader.VirtuePoints += virtue;
                await Team.SendAsync(new MsgTalk(Identity, TalkChannel.Team, Color.White,
                                                 string.Format(Language.StrAwardVirtue, Team.Leader.Name, virtue)));

                if (Team.Leader.SyndicateIdentity != 0)
                {
                    Team.Leader.SyndicateMember.GuideDonation += 1;
                    Team.Leader.SyndicateMember.GuideTotalDonation += 1;
                    await Team.Leader.SyndicateMember.SaveAsync();
                }
            }

            Experience = (ulong) amount;
            await SetAttributesAsync(ClientUpdateType.Experience, Experience);
            return true;
        }

        public async Task UpLevelEventAsync()
        {
            await GameAction.ExecuteActionAsync(USER_UPLEV_ACTION, this, this, null, string.Empty);

            if (Team != null)
                await Team.SyncFamilyBattlePowerAsync();

            if (ApprenticeCount > 0)
                await SynchroApprenticesSharedBattlePowerAsync();
        }

        public long CalculateExpBall(int amount = EXPBALL_AMOUNT)
        {
            long exp = 0;

            if (Level >= ExperienceManager.GetLevelLimit())
                return 0;

            byte level = Level;
            if (Experience > 0)
            {
                double pct = 1.00 - Experience / (double) ExperienceManager.GetLevelExperience(Level).Exp;
                if (amount > pct * ExperienceManager.GetLevelExperience(Level).UpLevTime)
                {
                    amount -= (int) (pct * ExperienceManager.GetLevelExperience(Level).UpLevTime);
                    exp += (long) (ExperienceManager.GetLevelExperience(Level).Exp - Experience);
                    level++;
                }
            }

            while (amount > ExperienceManager.GetLevelExperience(level).UpLevTime)
            {
                amount -= ExperienceManager.GetLevelExperience(level).UpLevTime;
                exp += (long) ExperienceManager.GetLevelExperience(level).Exp;

                if (level >= ExperienceManager.GetLevelLimit())
                    return exp;
                level++;
            }

            exp += (long) (amount / (double) ExperienceManager.GetLevelExperience(Level).UpLevTime *
                           ExperienceManager.GetLevelExperience(Level).Exp);
            return exp;
        }

        public (int Level, ulong Experience) PreviewExpBallUsage(int amount = EXPBALL_AMOUNT)
        {
            long expBallExp = (long) Experience + CalculateExpBall(amount);
            byte newLevel = Level;
            while (newLevel < MAX_UPLEV && expBallExp >= (long) ExperienceManager.GetLevelExperience(newLevel).Exp)
            {
                DbLevelExperience dbExp = ExperienceManager.GetLevelExperience(newLevel);
                expBallExp -= (long) dbExp.Exp;
                newLevel++;
                if (newLevel < ExperienceManager.GetLevelLimit()) continue;
                expBallExp = 0;
                break;
            }

            return (newLevel, (ulong) expBallExp);
        }

        public async Task IncrementExpBallAsync()
        {
            mDbObject.ExpBallUsage = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
            mDbObject.ExpBallNum += 1;

            mDbObject.MentorOpportunity += 10;
            await SynchroAttributesAsync(ClientUpdateType.EnlightenPoints, EnlightenPoints);
        }

        public bool CanUseExpBall()
        {
            if (Level >= ExperienceManager.GetLevelLimit())
                return false;

            if (mDbObject.ExpBallUsage < uint.Parse(DateTime.Now.ToString("yyyyMMdd")))
            {
                mDbObject.ExpBallNum = 0;
                return true;
            }

            return mDbObject.ExpBallNum < 10;
        }

        #endregion

        #region Life and Mana

        public override uint Life
        {
            get => mDbObject.HealthPoints;
            set => mDbObject.HealthPoints = (ushort) Math.Min(MaxLife, value);
        }

        public override uint MaxLife
        {
            get
            {
                if (Transformation != null)
                    return (uint) Transformation.MaxLife;

                var result = (uint) (Vitality * 24);
                switch (Profession)
                {
                    case 11:
                        result = (uint) (result * 1.05d);
                        break;
                    case 12:
                        result = (uint) (result * 1.08d);
                        break;
                    case 13:
                        result = (uint) (result * 1.10d);
                        break;
                    case 14:
                        result = (uint) (result * 1.12d);
                        break;
                    case 15:
                        result = (uint) (result * 1.15d);
                        break;
                }

                result += (uint) ((Strength + Agility + Spirit) * 3);

                for (var pos = Item.ItemPosition.EquipmentBegin;
                     pos <= Item.ItemPosition.EquipmentEnd;
                     pos++)
                    result += (uint) (UserPackage[pos]?.Life ?? 0);

                return result;
            }
        }

        public override uint Mana
        {
            get => mDbObject.ManaPoints;
            set =>
                mDbObject.ManaPoints = (ushort) Math.Min(MaxMana, value);
        }

        public override uint MaxMana
        {
            get
            {
                var result = (uint) (Spirit * 5);
                switch (Profession)
                {
                    case 132:
                    case 142:
                        result *= 3;
                        break;
                    case 133:
                    case 143:
                        result *= 4;
                        break;
                    case 134:
                    case 144:
                        result *= 5;
                        break;
                    case 135:
                    case 145:
                        result *= 6;
                        break;
                }

                for (var pos = Item.ItemPosition.EquipmentBegin;
                     pos <= Item.ItemPosition.EquipmentEnd;
                     pos++)
                    result += (uint) (UserPackage[pos]?.Mana ?? 0);

                return result;
            }
        }

        #endregion

        #region Screen

        public async Task BroadcastRoomMsgAsync(string message, TalkChannel channel = TalkChannel.TopLeft,
                                                Color? color = null, bool self = true)
        {
            await BroadcastRoomMsgAsync(new MsgTalk(Identity, channel, color ?? Color.Red, message), self);
        }

        public override async Task BroadcastRoomMsgAsync(IPacket msg, bool self)
        {
            await Screen.BroadcastRoomMsgAsync(msg, self);
        }

        #endregion

        #region Map and Position

        public override GameMap Map { get; protected set; }

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

        public uint RecordMapIdentity
        {
            get => mDbObject.MapID;
            set => mDbObject.MapID = value;
        }

        public ushort RecordMapX
        {
            get => mDbObject.X;
            set => mDbObject.X = value;
        }

        public ushort RecordMapY
        {
            get => mDbObject.Y;
            set => mDbObject.Y = value;
        }

        /// <summary>
        /// </summary>
        public override async Task EnterMapAsync()
        {
            Map = MapManager.GetMap(mIdMap);
            if (Map != null)
            {
                await Map.AddAsync(this);
                await Map.SendMapInfoAsync(this);
                await Screen.SynchroScreenAsync();

                mRespawn.Startup(10);

                if (Map.IsTeamDisable() && Team != null)
                {
                    if (Team.Leader.Identity == Identity)
                        await Team.DismissAsync(this);
                    else await Team.DismissMemberAsync(this);
                }

                if (CurrentEvent == null)
                {
                    GameEvent @event = EventManager.GetEvent(mIdMap);
                    if (@event != null)
                        await SignInEventAsync(@event);
                }

                if (Team != null)
                    await Team.SyncFamilyBattlePowerAsync();
            }
            else
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Invalid map {mIdMap} for user {Identity} {Name}");
                Client?.Disconnect();
            }
        }

        /// <summary>
        /// </summary>
        public override async Task LeaveMapAsync()
        {
            BattleSystem.ResetBattle();
            await MagicData.AbortMagicAsync(false);
            await KillCallPetAsync(true);
            StopMining();

            if (Map != null)
            {
                await Map.RemoveAsync(Identity);

                if (CurrentEvent != null && CurrentEvent.Map.Identity != 0 && CurrentEvent.Map.Identity == Map.Identity)
                    await SignOutEventAsync();
            }

            if (Team != null)
                await Team.SyncFamilyBattlePowerAsync();

            await Screen.ClearAsync();
        }

        public override async Task ProcessOnMoveAsync()
        {
            StopMining();

            if (CurrentEvent != null)
                await CurrentEvent.OnMoveAsync(this);

            if (QueryStatus(StatusSet.LUCKY_DIFFUSE) != null)
                foreach (Character user in Screen.Roles.Values
                                                 .Where(x => x.IsPlayer() &&
                                                             x.QueryStatus(StatusSet.LUCKY_ABSORB)?.CasterId ==
                                                             Identity).Cast<Character>())
                    await user.DetachStatusAsync(StatusSet.LUCKY_DIFFUSE);

            mLuckyAbsorbStart.Clear();
            mIdLuckyTarget = 0;

            mRespawn.Clear();

            await base.ProcessOnMoveAsync();
        }

        public override async Task ProcessAfterMoveAsync()
        {
            if (IsAlive) UpdateVigorTimer();

            await base.ProcessAfterMoveAsync();
        }

        public override async Task ProcessOnAttackAsync()
        {
            StopMining();

            if (CurrentEvent != null)
                await CurrentEvent.OnAttackAsync(this);

            mRespawn.Clear();

            UpdateVigorTimer();

            await base.ProcessOnAttackAsync();
        }

        public async Task SavePositionAsync()
        {
            if (!Map.IsRecordDisable())
            {
                mDbObject.X = mPosX;
                mDbObject.Y = mPosY;
                mDbObject.MapID = mIdMap;
                await SaveAsync();
            }
        }

        public async Task SavePositionAsync(uint idMap, ushort x, ushort y)
        {
            GameMap map = MapManager.GetMap(idMap);
            if (map?.IsRecordDisable() == false)
            {
                mDbObject.X = x;
                mDbObject.Y = y;
                mDbObject.MapID = idMap;
                await SaveAsync();
            }
        }

        public async Task<bool> FlyMapAsync(uint idMap, int x, int y)
        {
            if (Map == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning, "FlyMap user not in map");
                return false;
            }

            if (idMap == 0)
                idMap = MapIdentity;

            GameMap newMap = MapManager.GetMap(idMap);
            if (newMap == null || !newMap.IsValidPoint(x, y))
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"FlyMap user fly invalid position {idMap}[{x},{y}]");
                return false;
            }

            if (!newMap.IsStandEnable(x, y))
            {
                bool succ = false;
                for (int i = 0; i < 8; i++)
                {
                    int testX = x + GameMapData.WalkXCoords[i];
                    int testY = y + GameMapData.WalkYCoords[i];

                    if (newMap.IsStandEnable(testX, testY))
                    {
                        x = testX;
                        y = testY;
                        succ = true;
                        break;
                    }
                }

                if (!succ)
                {
                    newMap = MapManager.GetMap(1002);
                    x = 430;
                    y = 378;
                }
            }

            try
            {
                await LeaveMapAsync();

                mIdMap = newMap.Identity;
                MapX = (ushort) x;
                MapY = (ushort) y;

                await SendAsync(new MsgAction
                {
                    Identity = Identity,
                    Command = newMap.MapDoc,
                    X = MapX,
                    Y = MapY,
                    Action = MsgAction<Client>.ActionType.MapTeleport,
                    Direction = (ushort) Direction
                });

                await Kernel.BroadcastWorldMsgAsync(new MsgAiAction
                {
                    Action = MsgAiAction<AiClient>.AiAction.FlyMap,
                    Data = (int) Identity,
                    Param = (int) newMap.Identity,
                    X = MapX,
                    Y = MapY
                });

                await EnterMapAsync();
            }
            catch
            {
                await Log.WriteLogAsync(LogLevel.Error, "FlyMap error");
            }
            return true;
        }

        public Role QueryRole(uint idRole)
        {
            return Map.QueryAroundRole(this, idRole);
        }

        public bool IsJumpPass(int x, int y, int alt)
        {
            var setLine = new List<Point>();
            Calculations.DDALineEx(MapX, MapY, x, y, ref setLine);

            if (x != setLine[setLine.Count - 1].X)
                return false;
            if (y != setLine[setLine.Count - 1].Y)
                return false;

            var fAlt = (float) (Map.GetFloorAlt(MapX, MapY) + alt + 0.5);

            foreach (Point point in setLine)
                if (Map.IsAltOver(point.X, point.Y, (int) fAlt))
                    return false;

            return true;
        }

        public bool IsArrowPass(int x, int y, int alt)
        {
            //{
            //    var setLine = new List<Point>();
            //    Calculations.DDALineEx(MapX, MapY, x, y, ref setLine);

            //    if (x != setLine[setLine.Count - 1].X)
            //        return false;
            //    if (y != setLine[setLine.Count - 1].Y)
            //        return false;

            //    var fAlt = (float) (Map.GetFloorAlt(MapX, MapY) + alt + 0.5);
            //    float fDelta = (Map.GetFloorAlt(x, y) - fAlt) / setLine.Count;

            //    foreach (Point point in setLine)
            //    {
            //        if (Map.IsAltOver(point.X, point.Y, (int) fAlt))
            //            return false;
            //        fAlt += fDelta;
            //    }

            //    return true;
            //}
            return true;
        }

        #endregion

        #region Movement

        public async Task<bool> SynPositionAsync(ushort x, ushort y, int nMaxDislocation)
        {
            if (nMaxDislocation <= 0 || x == 0 && y == 0) // ignore in this condition
                return true;

            int nDislocation = GetDistance(x, y);
            if (nDislocation >= nMaxDislocation)
                return false;

            if (nDislocation <= 0)
                return true;

            if (IsGm())
                await SendAsync($"syn move: ({MapX},{MapY})->({x},{y})", TalkChannel.Talk, Color.Red);

            if (!Map.IsValidPoint(x, y))
                return false;

            await ProcessOnMoveAsync();
            await JumpPosAsync(x, y);
            await Screen.BroadcastRoomMsgAsync(new MsgAction
            {
                Identity = Identity,
                Action = MsgAction<Client>.ActionType.Kickback,
                ArgumentX = x,
                ArgumentY = y,
                Command = (uint) ((y << 16) | x),
                Direction = (ushort) Direction
            });

            return true;
        }

        public Task KickbackAsync()
        {
            return SendAsync(new MsgAction
            {
                Identity = Identity,
                Direction = (ushort) Direction,
                Map = MapIdentity,
                X = MapX,
                Y = MapY,
                Action = MsgAction<Client>.ActionType.Kickback,
                Timestamp = (uint) Environment.TickCount
            });
        }

        #endregion

        #region Currency

        public uint Silvers
        {
            get => mDbObject?.Silver ?? 0;
            set => mDbObject.Silver = value;
        }

        public uint ConquerPoints
        {
            get => mDbObject?.ConquerPoints ?? 0;
            set => mDbObject.ConquerPoints = value;
        }

        public uint ConquerPointsBound
        {
            get => mDbObject?.ConquerPointsBound ?? 0;
            set => mDbObject.ConquerPointsBound = value;
        }

        public uint StorageMoney
        {
            get => mDbObject?.StorageMoney ?? 0;
            set => mDbObject.StorageMoney = value;
        }

        public uint StudyPoints
        {
            get => mDbObject?.Cultivation ?? 0;
            set => mDbObject.Cultivation = value;
        }

        public uint ChiPoints
        {
            get => mDbObject?.StrengthValue ?? 0;
            set => mDbObject.StrengthValue = value;
        }

        public async Task<bool> ChangeMoneyAsync(int amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardMoneyAsync(amount);
                return true;
            }

            if (amount < 0) return await SpendMoneyAsync(amount * -1, notify);
            return false;
        }

        public async Task AwardMoneyAsync(int amount)
        {
            Silvers = (uint) (Silvers + amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.Money, Silvers);
        }

        public async Task<bool> SpendMoneyAsync(int amount, bool notify = false)
        {
            if (amount > Silvers)
            {
                if (notify)
                    await SendAsync(Language.StrNotEnoughMoney, TalkChannel.TopLeft, Color.Red);
                return false;
            }

            Silvers = (uint) (Silvers - amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.Money, Silvers);
            return true;
        }

        public async Task<bool> ChangeConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardConquerPointsAsync(amount);
                return true;
            }

            if (amount < 0) return await SpendConquerPointsAsync(amount * -1, notify);
            return false;
        }

        public async Task AwardConquerPointsAsync(int amount)
        {
            ConquerPoints = (uint) (ConquerPoints + amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.ConquerPoints, ConquerPoints);
        }

        public async Task<bool> SpendConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > ConquerPoints)
            {
                if (notify)
                    await SendAsync(Language.StrNotEnoughEmoney, TalkChannel.TopLeft, Color.Red);
                return false;
            }

            ConquerPoints = (uint) (ConquerPoints - amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.ConquerPoints, ConquerPoints);
            return true;
        }

        public async Task<bool> SpendConquerPointsAsync(int amount, bool bound, bool notify)
        {
            if (!bound || ConquerPointsBound == 0)
                return await SpendConquerPointsAsync(amount, notify);

            if (amount > ConquerPoints + ConquerPointsBound)
            {
                if (notify)
                    await SendAsync(Language.StrNotEnoughEmoney, TalkChannel.TopLeft, Color.Red);
                return false;
            }

            if (ConquerPointsBound > amount)
                return await SpendBoundConquerPointsAsync(amount, notify);

            var remain = (int) (amount - ConquerPointsBound);
            await SpendBoundConquerPointsAsync((int) ConquerPointsBound);
            await SpendConquerPointsAsync(remain);
            return true;
        }

        public async Task<bool> ChangeBoundConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > 0)
            {
                await AwardBoundConquerPointsAsync(amount);
                return true;
            }

            if (amount < 0) return await SpendBoundConquerPointsAsync(amount * -1, notify);
            return false;
        }

        public async Task AwardBoundConquerPointsAsync(int amount)
        {
            ConquerPointsBound = (uint) (ConquerPointsBound + amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.BoundConquerPoints, ConquerPointsBound);
        }

        public async Task<bool> SpendBoundConquerPointsAsync(int amount, bool notify = false)
        {
            if (amount > ConquerPoints)
            {
                if (notify)
                    await SendAsync(Language.StrNotEnoughEmoney, TalkChannel.TopLeft, Color.Red);
                return false;
            }

            ConquerPointsBound = (uint) (ConquerPointsBound - amount);
            await SaveAsync();
            await SynchroAttributesAsync(ClientUpdateType.BoundConquerPoints, ConquerPointsBound);
            return true;
        }

        public async Task<bool> ChangeCultivationAsync(int amount)
        {
            if (amount > 0)
            {
                await AwardCultivationAsync(amount);
                return true;
            }

            if (amount < 0) return await SpendCultivationAsync(amount * -1);
            return false;
        }

        public async Task AwardCultivationAsync(int amount)
        {
            StudyPoints = (uint) (StudyPoints + amount);
            await SaveAsync();

            //await SendAsync(new MsgSubPro
            //{
            //    Action = AstProfAction.UpdateStudy,
            //    Points = StudyPoints,
            //    Study = (ulong)amount
            //});
        }

        public async Task<bool> SpendCultivationAsync(int amount)
        {
            StudyPoints = (uint) (StudyPoints - amount);
            await SaveAsync();
            //await SendAsync(new MsgSubPro
            //{
            //    Action = AstProfAction.UpdateStudy,
            //    Points = StudyPoints
            //});
            return true;
        }

        public async Task<bool> ChangeStrengthValueAsync(int amount)
        {
            if (amount > 0)
            {
                await AwardStrengthValueAsync(amount);
                return true;
            }

            if (amount < 0) return await SpendStrengthValueAsync(amount * -1);
            return false;
        }

        public async Task AwardStrengthValueAsync(int amount)
        {
            ChiPoints = (uint) (ChiPoints + amount);
            await SaveAsync();
        }

        public async Task<bool> SpendStrengthValueAsync(int amount)
        {
            ChiPoints = (uint) (ChiPoints - amount);
            await SaveAsync();
            return true;
        }

        #endregion

        #region Pk

        public PkModeType PkMode { get; set; }

        public ushort PkPoints
        {
            get => mDbObject?.KillPoints ?? 0;
            set => mDbObject.KillPoints = value;
        }

        public Task SetPkModeAsync(PkModeType mode = PkModeType.Capture)
        {
            PkMode = mode;
            return SendAsync(new MsgAction
            {
                Identity = Identity,
                Action = MsgAction<Client>.ActionType.CharacterPkMode,
                Command = (uint) PkMode
            });
        }

        public async Task ProcessPkAsync(Character target)
        {
            if (!Map.IsPkField() && !Map.IsPkGameMap() && !Map.IsSynMap() && !Map.IsPrisionMap())
                if (!Map.IsDeadIsland() && !target.IsEvil())
                {
                    var nAddPk = 10;
                    if (target.IsNewbie() && !IsNewbie())
                    {
                        nAddPk = 20;
                    }
                    else
                    {
                        if (Syndicate?.IsEnemy(target.SyndicateIdentity) == true)
                            nAddPk = 3;
                        else if (IsEnemy(target.Identity))
                            nAddPk = 5;
                        if (target.PkPoints > 29)
                            nAddPk /= 2;
                    }

                    int deltaLevel = Level - target.Level;
                    var synPkPoints = 10;
                    if (deltaLevel > 30)
                        synPkPoints = 1;
                    else if (deltaLevel > 20)
                        synPkPoints = 2;
                    else if (deltaLevel > 10)
                        synPkPoints = 3;
                    else if (deltaLevel > 0)
                        synPkPoints = 5;

                    if (SyndicateIdentity != 0)
                    {
                        SyndicateMember.PkDonation += synPkPoints;
                        SyndicateMember.PkTotalDonation += synPkPoints;
                        await SyndicateMember.SaveAsync().ConfigureAwait(false);
                    }

                    if (target.SyndicateIdentity != 0)
                    {
                        target.SyndicateMember.PkDonation -= synPkPoints;
                        await target.SyndicateMember.SaveAsync().ConfigureAwait(false);
                    }

                    if (SyndicateIdentity != 0 && target.SyndicateIdentity != 0)
                    {
                        if (SyndicateIdentity == target.SyndicateIdentity)
                        {
                            await Syndicate.SendAsync(
                                string.Format(Language.StrSyndicateSameKill, SyndicateRankName, Name,
                                              target.SyndicateRankName, target.Name, Map.Name), 0, Color.White);
                        }
                        else
                        {
                            await Syndicate.SendAsync(string.Format(Language.StrSyndicateKill, SyndicateRankName, Name,
                                                                    target.Name, target.SyndicateRankName,
                                                                    target.SyndicateName, Map.Name));
                            await target.Syndicate.SendAsync(string.Format(Language.StrSyndicateBeKill, Name,
                                                                           SyndicateRankName, SyndicateName,
                                                                           target.SyndicateRankName, target.Name,
                                                                           Map.Name));
                        }
                    }

                    await AddAttributesAsync(ClientUpdateType.PkPoints, nAddPk);

                    await SetCrimeStatusAsync(90);

                    if (PkPoints > 29)
                        await SendAsync(Language.StrKillingTooMuch);
                }
        }

        public override async Task<bool> CheckCrimeAsync(Role target)
        {
            if (target == null || !target.IsAlive) return false;
            if (!target.IsEvil() && !target.IsMonster() && !(target is DynamicNpc))
            {
                if (!Map.IsTrainingMap() && !Map.IsDeadIsland()
                                         && !Map.IsPrisionMap()
                                         && !Map.IsFamilyMap()
                                         && !Map.IsPkGameMap()
                                         && !Map.IsPkField()
                                         && !Map.IsSynMap()
                                         && !Map.IsFamilyMap())
                    await SetCrimeStatusAsync(30);
                return true;
            }

            if (target is Monster mob && (mob.IsGuard() || mob.IsPkKiller()))
            {
                await SetCrimeStatusAsync(15);
                return true;
            }

            return false;
        }

        #endregion

        #region Game Action

        private readonly List<uint> m_setTaskId = new();

        public uint InteractingItem { get; set; }
        public uint InteractingNpc { get; set; }

        public bool CheckItem(DbTask task)
        {
            //if (task.Itemname1.Length > 0)
            //{
            //    if (UserPackage[task.Itemname1] == null)
            //        return false;

            //    if (task.Itemname2.Length > 0)
            //    {
            //        if (UserPackage[task.Itemname2] == null)
            //            return false;
            //    }
            //}

            return true;
        }

        public void CancelInteraction()
        {
            m_setTaskId.Clear();
            InteractingItem = 0;
            InteractingNpc = 0;
        }

        public byte PushTaskId(uint idTask)
        {
            if (idTask != 0 && m_setTaskId.Count < MAX_MENUTASKSIZE)
            {
                m_setTaskId.Add(idTask);
                return (byte) m_setTaskId.Count;
            }

            return 0;
        }

        public void ClearTaskId()
        {
            m_setTaskId.Clear();
        }

        public uint GetTaskId(int idx)
        {
            return idx > 0 && idx <= m_setTaskId.Count ? m_setTaskId[idx - 1] : 0u;
        }

        public async Task<bool> TestTaskAsync(DbTask task)
        {
            if (task == null) return false;

            try
            {
                if (!CheckItem(task))
                    return false;

                if (Silvers < task.Money)
                    return false;

                if (task.Profession != 0 && Profession != task.Profession)
                    return false;

                if (task.Sex != 0 && task.Sex != 999 && task.Sex != Gender)
                    return false;

                if (PkPoints < task.MinPk || PkPoints > task.MaxPk)
                    return false;

                if (task.Marriage >= 0)
                {
                    if (task.Marriage == 0 && MateIdentity != 0)
                        return false;
                    if (task.Marriage == 1 && MateIdentity == 0)
                        return false;
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Test task error");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }

            return true;
        }

        public async Task AddTaskMaskAsync(int idx)
        {
            if (idx < 0 || idx >= 32)
                return;

            mDbObject.TaskMask |= 1u << idx;
            await SaveAsync();
        }

        public async Task ClearTaskMaskAsync(int idx)
        {
            if (idx < 0 || idx >= 32)
                return;

            mDbObject.TaskMask &= ~(1u << idx);
            await SaveAsync();
        }

        public bool CheckTaskMask(int idx)
        {
            if (idx < 0 || idx >= 32)
                return false;
            return (mDbObject.TaskMask & (1u << idx)) != 0;
        }

        #endregion

        #region Merchant

        public int Merchant => mDbObject.Business == null ? 0 : IsMerchant() ? 255 : 1;

        public int BusinessManDays => (int) (mDbObject.Business == null
                                                 ? 0
                                                 : Math.Ceiling((mDbObject.Business.Value - DateTime.Now).TotalDays));


        public bool IsMerchant()
        {
            return mDbObject.Business.HasValue && mDbObject.Business.Value < DateTime.Now;
        }

        public bool IsAwaitingMerchantStatus()
        {
            return mDbObject.Business.HasValue && mDbObject.Business.Value > DateTime.Now;
        }

        public async Task<bool> SetMerchantAsync()
        {
            if (IsMerchant())
                return false;

            if (Level <= 30 && Metempsychosis == 0)
            {
                mDbObject.Business = DateTime.Now;
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 255);
            }
            else
            {
                mDbObject.Business = DateTime.Now.AddDays(5);
            }

            return await SaveAsync();
        }

        public async Task RemoveMerchantAsync()
        {
            mDbObject.Business = null;
            await SynchroAttributesAsync(ClientUpdateType.Merchant, 0);
            await SaveAsync();
        }

        public async Task SendMerchantAsync()
        {
            if (IsMerchant())
            {
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 255);
                return;
            }

            if (IsAwaitingMerchantStatus())
            {
                await SynchroAttributesAsync(ClientUpdateType.Merchant, 1);
                await SendAsync(new MsgInteract
                {
                    Action = MsgInteractType.MerchantProgress,
                    Command = BusinessManDays
                });
                return;
            }

            if (Level <= 30 && Metempsychosis == 0)
            {
                await SendAsync(new MsgInteract
                {
                    Action = MsgInteractType.InitialMerchant
                });
                return;
            }

            await SynchroAttributesAsync(ClientUpdateType.Merchant, 0);
        }

        #endregion

        #region Flower

        public bool CanRefreshFlowerRank => mFlowerRankRefresh.ToNextTime();
        public uint FlowerCharm { get; set; }
        public uint FairyType { get; set; }

        public DateTime? SendFlowerTime
        {
            get => mDbObject.SendFlowerDate;
            set => mDbObject.SendFlowerDate = value;
        }

        public uint FlowerRed
        {
            get => mDbObject.FlowerRed;
            set => mDbObject.FlowerRed = value;
        }

        public uint FlowerWhite
        {
            get => mDbObject.FlowerWhite;
            set => mDbObject.FlowerWhite = value;
        }

        public uint FlowerOrchid
        {
            get => mDbObject.FlowerOrchid;
            set => mDbObject.FlowerOrchid = value;
        }

        public uint FlowerTulip
        {
            get => mDbObject.FlowerTulip;
            set => mDbObject.FlowerTulip = value;
        }

        #endregion

        #region Marriage

        public bool IsMate(Character user)
        {
            return user.Identity == MateIdentity;
        }

        public bool IsMate(uint idMate)
        {
            return idMate == MateIdentity;
        }

        #endregion

        #region Requests

        private int mRequestData;

        public void SetRequest(RequestType type, uint target, int data = 0)
        {
            mDicRequests.TryRemove(type, out _);
            if (target == 0)
                return;

            mRequestData = data;
            mDicRequests.TryAdd(type, target);
        }

        public uint QueryRequest(RequestType type)
        {
            return mDicRequests.TryGetValue(type, out uint value) ? value : 0;
        }

        public int QueryRequestData(RequestType type)
        {
            if (mDicRequests.TryGetValue(type, out _))
                return mRequestData;
            return 0;
        }

        public uint PopRequest(RequestType type)
        {
            if (mDicRequests.TryRemove(type, out uint value))
            {
                mRequestData = 0;
                return value;
            }
            return 0;
        }

        #endregion

        #region Multiple Exp

        public bool HasMultipleExp =>
            mDbObject.ExperienceMultiplier > 1 && mDbObject.ExperienceExpires >= DateTime.Now;

        public float ExperienceMultiplier => !HasMultipleExp || mDbObject.ExperienceMultiplier <= 0
                                                 ? 1f
                                                 : mDbObject.ExperienceMultiplier;

        public async Task SendMultipleExpAsync()
        {
            if (RemainingExperienceSeconds > 0)
                await SynchroAttributesAsync(ClientUpdateType.DoubleExpTimer, RemainingExperienceSeconds);
        }

        public uint RemainingExperienceSeconds
        {
            get
            {
                DateTime now = DateTime.Now;
                if (mDbObject.ExperienceExpires < now)
                {
                    mDbObject.ExperienceMultiplier = 1;
                    mDbObject.ExperienceExpires = null;
                    return 0;
                }

                return (uint) ((mDbObject.ExperienceExpires - now)?.TotalSeconds ?? 0);
            }
        }

        public async Task<bool> SetExperienceMultiplierAsync(uint nSeconds, float nMultiplier = 2f)
        {
            mDbObject.ExperienceExpires = DateTime.Now.AddSeconds(nSeconds);
            mDbObject.ExperienceMultiplier = nMultiplier;
            await SendMultipleExpAsync();
            return true;
        }

        #endregion

        #region Heaven Blessing

        public async Task SendBlessAsync()
        {
            if (IsBlessed)
            {
                DateTime now = DateTime.Now;
                await SynchroAttributesAsync(ClientUpdateType.HeavensBlessing,
                                             (uint) (HeavenBlessingExpires - now).TotalSeconds);

                if (Map != null && !Map.IsTrainingMap())
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 0);
                else
                    await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 1);

                await AttachStatusAsync(this, StatusSet.HEAVEN_BLESS, 0,
                                        (int) (HeavenBlessingExpires - now).TotalSeconds, 0, 0);
            }
        }

        /// <summary>
        ///     This method will update the user blessing time.
        /// </summary>
        /// <param name="amount">The amount of minutes to be added.</param>
        /// <returns>If the heaven blessing has been added successfully.</returns>
        public async Task<bool> AddBlessingAsync(uint amount)
        {
            DateTime now = DateTime.Now;
            if (mDbObject.HeavenBlessing != null && mDbObject.HeavenBlessing > now)
                mDbObject.HeavenBlessing = mDbObject.HeavenBlessing.Value.AddHours(amount);
            else
                mDbObject.HeavenBlessing = now.AddHours(amount);

            await SendBlessAsync();
            return true;
        }

        public DateTime HeavenBlessingExpires => mDbObject.HeavenBlessing ?? DateTime.MinValue;

        public bool IsBlessed => mDbObject.HeavenBlessing > DateTime.Now;

        #endregion

        #region Lucky

        public Task ChangeLuckyTimerAsync(int value)
        {
            ulong ms = 0;

            mLuckyTimeCount += value;
            if (mLuckyTimeCount > 0)
                mDbObject.LuckyTime = DateTime.Now.AddSeconds(mLuckyTimeCount);

            if (IsLucky)
                ms = (ulong) (mDbObject.LuckyTime.Value - DateTime.Now).TotalSeconds * 1000UL;

            return SynchroAttributesAsync(ClientUpdateType.LuckyTimeTimer, ms);
        }

        public bool IsLucky => mDbObject.LuckyTime.HasValue && mDbObject.LuckyTime.Value > DateTime.Now;

        public async Task SendLuckAsync()
        {
            if (IsLucky)
                await SynchroAttributesAsync(ClientUpdateType.LuckyTimeTimer,
                                             (ulong) (mDbObject.LuckyTime.Value - DateTime.Now).TotalSeconds * 1000UL);
        }

        #endregion

        #region XP and Stamina

        public byte Energy { get; private set; } = DEFAULT_USER_ENERGY;

        public byte MaxEnergy => (byte) (IsBlessed ? 150 : 100);

        public byte XpPoints;

        public async Task ProcXpValAsync()
        {
            if (!IsAlive)
            {
                await ClsXpValAsync();
                return;
            }

            IStatus pStatus = QueryStatus(StatusSet.START_XP);
            if (pStatus != null)
                return;

            if (XpPoints >= 100)
            {
                await BurstXpAsync();
                await SetXpAsync(0);
                mXpPoints.Update();
            }
            else
            {
                if (Map != null && Map.IsBoothEnable())
                    return;
                await AddXpAsync(1);
            }
        }

        public async Task<bool> BurstXpAsync()
        {
            if (XpPoints < 100)
                return false;

            IStatus pStatus = QueryStatus(StatusSet.START_XP);
            if (pStatus != null)
                return true;

            await AttachStatusAsync(this, StatusSet.START_XP, 0, 20, 0, 0);
            return true;
        }

        public async Task SetXpAsync(byte nXp)
        {
            if (nXp > 100)
                return;
            await SetAttributesAsync(ClientUpdateType.XpCircle, nXp);
        }

        public async Task AddXpAsync(byte nXp)
        {
            if (nXp <= 0 || !IsAlive || QueryStatus(StatusSet.START_XP) != null)
                return;
            await AddAttributesAsync(ClientUpdateType.XpCircle, nXp);
        }

        public async Task ClsXpValAsync()
        {
            XpPoints = 0;
            await StatusSet.DelObjAsync(StatusSet.START_XP);
        }

        public async Task FinishXpAsync()
        {
            //int currentPoints = RoleManager.GetSupermanPoints(Identity);
            //if (KoCount >= 25
            //    && currentPoints < KoCount)
            //{
            //    await RoleManager.AddOrUpdateSupermanAsync(Identity, KoCount);
            //    int rank = RoleManager.GetSupermanRank(Identity);
            //    if (rank < 100)
            //        await RoleManager.BroadcastMsgAsync(string.Format(Language.StrSupermanBroadcast, Name, KoCount, rank), TalkChannel.Talk);
            //}
            KoCount = 0;
        }

        #endregion

        #region VIP

        private readonly TimeOut m_vipCmdTp = new(120);

        public bool IsVipTeleportEnable()
        {
            return m_vipCmdTp.ToNextTime();
        }

        public uint BaseVipLevel => Math.Min(6, Math.Max(0, VipLevel));

        public uint VipLevel
        {
            get =>
                mDbObject.VipExpiration.HasValue && mDbObject.VipExpiration > DateTime.Now
                    ? mDbObject.VipLevel
                    : 0;
            set => mDbObject.VipLevel = value;
        }

        public bool HasVip => mDbObject.VipExpiration.HasValue && mDbObject.VipExpiration > DateTime.Now;

        public DateTime VipExpiration
        {
            get => mDbObject.VipExpiration ?? DateTime.MinValue;
            set => mDbObject.VipExpiration = value;
        }

        public VipFlags UserVipFlag
        {
            get
            {
                switch (BaseVipLevel)
                {
                    case 1:
                        return VipFlags.VipOne;
                    case 2:
                        return VipFlags.VipTwo;
                    case 3:
                        return VipFlags.VipThree;
                    case 4:
                        return VipFlags.VipFour;
                    case 5:
                        return VipFlags.VipFive;
                    case 6:
                        return VipFlags.VipSix;
                }

                return 0;
            }
        }

        #endregion

        #region Attributes Set and Add

        public override async Task<bool> AddAttributesAsync(ClientUpdateType type, long value)
        {
            var screen = false;
            switch (type)
            {
                case ClientUpdateType.Level:
                {
                    if (value < 0)
                        return false;

                    screen = true;
                    value = Level = (byte) Math.Max(1, Math.Min(MAX_UPLEV, Level + value));

                    if (Syndicate != null) SyndicateMember.Level = Level;

                    if (Family != null) FamilyMember.Level = Level;

                    await GameAction.ExecuteActionAsync(USER_UPLEV_ACTION, this, null, null, string.Empty);
                    break;
                }

                case ClientUpdateType.Experience:
                {
                    if (value < 0)
                        Experience = Math.Max(0, Experience - (ulong) (value * -1));
                    else
                        Experience += (ulong) value;

                    value = (long) Experience;
                    break;
                }

                case ClientUpdateType.Strength:
                {
                    if (value < 0)
                        return false;

                    value = Strength = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, Strength + value));
                    break;
                }

                case ClientUpdateType.Agility:
                {
                    if (value < 0)
                        return false;

                    value = Agility = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, Agility + value));
                    break;
                }

                case ClientUpdateType.Vitality:
                {
                    if (value < 0)
                        return false;

                    value = Vitality = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, Vitality + value));
                    break;
                }

                case ClientUpdateType.Spirit:
                {
                    if (value < 0)
                        return false;

                    value = Spirit = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, Spirit + value));
                    break;
                }

                case ClientUpdateType.Atributes:
                {
                    if (value < 0)
                        return false;

                    value = AttributePoints = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, AttributePoints + value));
                    break;
                }

                case ClientUpdateType.XpCircle:
                {
                    if (value < 0)
                        XpPoints = (byte) Math.Max(0, XpPoints - value * -1);
                    else
                        XpPoints = (byte) Math.Max(0, XpPoints + value);

                    value = XpPoints;
                    break;
                }

                case ClientUpdateType.Stamina:
                {
                    if (value < 0)
                        Energy = (byte) Math.Max(0, Energy - value * -1);
                    else
                        Energy = (byte) Math.Max(0, Math.Min(MaxEnergy, Energy + value));

                    value = Energy;
                    break;
                }

                case ClientUpdateType.PkPoints:
                {
                    value = PkPoints = (ushort) Math.Max(0, Math.Min(PkPoints + value, ushort.MaxValue));
                    await CheckPkStatusAsync();
                    break;
                }

                case ClientUpdateType.Vigor:
                {
                    Vigor = Math.Max(0, Math.Min(MaxVigor, (int) value + Vigor));
                    await SendAsync(new MsgData
                    {
                        Action = MsgData<Client>.DataAction.SetMountMovePoint,
                        Year = Vigor
                    });
                    return true;
                }

                default:
                {
                    bool result = await base.AddAttributesAsync(type, value);
                    return result && await SaveAsync();
                }
            }

            await SaveAsync();
            await SynchroAttributesAsync(type, (ulong) value, screen);
            return true;
        }

        public override async Task<bool> SetAttributesAsync(ClientUpdateType type, ulong value)
        {
            var screen = false;
            switch (type)
            {
                case ClientUpdateType.Level:
                    screen = true;
                    Level = (byte) Math.Max(1, Math.Min(MAX_UPLEV, value));
                    break;

                case ClientUpdateType.Experience:
                    Experience = Math.Max(0, value);
                    break;

                case ClientUpdateType.XpCircle:
                    XpPoints = (byte) Math.Max(0, Math.Min(value, 100));
                    break;

                case ClientUpdateType.Stamina:
                    Energy = (byte) Math.Max(0, Math.Min(value, MaxEnergy));
                    break;

                case ClientUpdateType.Atributes:
                    AttributePoints = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, value));
                    break;

                case ClientUpdateType.PkPoints:
                    PkPoints = (ushort) Math.Max(0, Math.Min(ushort.MaxValue, value));
                    await CheckPkStatusAsync();
                    break;

                case ClientUpdateType.Mesh:
                    screen = true;
                    Mesh = (uint) value;
                    break;

                case ClientUpdateType.HairStyle:
                    screen = true;
                    Hairstyle = (ushort) value;
                    break;

                case ClientUpdateType.Strength:
                    value = Strength = (ushort) Math.Min(ushort.MaxValue, value);
                    break;

                case ClientUpdateType.Agility:
                    value = Agility = (ushort) Math.Min(ushort.MaxValue, value);
                    break;

                case ClientUpdateType.Vitality:
                    value = Vitality = (ushort) Math.Min(ushort.MaxValue, value);
                    break;

                case ClientUpdateType.Spirit:
                    value = Spirit = (ushort) Math.Min(ushort.MaxValue, value);
                    break;

                case ClientUpdateType.Class:
                    Profession = (byte) value;
                    break;

                case ClientUpdateType.Reborn:
                    Metempsychosis = (byte) value;
                    break;

                case ClientUpdateType.VipLevel:
                {
                    value = VipLevel = (uint) Math.Max(0, Math.Min(6, value));

                    if (VipLevel > 0)
                        await AttachStatusAsync(this, StatusSet.ORANGE_HALO_GLOW, 0,
                                                (int) (VipExpiration - DateTime.Now).TotalSeconds, 0, 0);
                    break;
                }

                case ClientUpdateType.Vigor:
                {
                    Vigor = Math.Max(0, Math.Min(MaxVigor, (int) value));
                    await SendAsync(new MsgData
                    {
                        Action = MsgData<Client>.DataAction.SetMountMovePoint,
                        Year = Vigor
                    });
                    return true;
                }

                default:
                    bool result = await base.SetAttributesAsync(type, value);
                    return result && await SaveAsync();
            }

            await SaveAsync();
            await SynchroAttributesAsync(type, value, screen);
            return true;
        }

        public async Task CheckPkStatusAsync()
        {
            //if (m_dbObject.KillPoints != value)
            {
                if (PkPoints > 99 && QueryStatus(StatusSet.BLACK_NAME) == null)
                {
                    await DetachStatusAsync(StatusSet.RED_NAME);
                    await AttachStatusAsync(this, StatusSet.BLACK_NAME, 0, int.MaxValue, 1, 0);
                }
                else if (PkPoints > 29 && PkPoints < 100 && QueryStatus(StatusSet.RED_NAME) == null)
                {
                    await DetachStatusAsync(StatusSet.BLACK_NAME);
                    await AttachStatusAsync(this, StatusSet.RED_NAME, 0, int.MaxValue, 1, 0);
                }
                else if (PkPoints < 30)
                {
                    await DetachStatusAsync(StatusSet.BLACK_NAME);
                    await DetachStatusAsync(StatusSet.RED_NAME);
                }
            }
        }

        #endregion

        #region Peerage

        public NobilityRank NobilityRank => PeerageManager.GetRanking(Identity);

        public int NobilityPosition => PeerageManager.GetPosition(Identity);

        public ulong NobilityDonation
        {
            get => mDbObject.Donation;
            set => mDbObject.Donation = value;
        }

        public async Task SendNobilityInfoAsync(bool broadcast = false)
        {
            MsgPeerage msg = new()
            {
                Action = NobilityAction.Info,
                DataLow = Identity
            };
            msg.Strings.Add($"{Identity} {NobilityDonation} {(int) NobilityRank:d} {NobilityPosition}");
            await SendAsync(msg);

            if (broadcast)
                await BroadcastRoomMsgAsync(msg, false);
        }

        #endregion

        #region Relation Packet

        public Task SendRelationAsync(Character target)
        {
            return SendAsync(new MsgRelation
            {
                SenderIdentity = target.Identity,
                Level = target.Level,
                BattlePower = target.BattlePower,
                IsSpouse = target.Identity == MateIdentity,
                IsTradePartner = IsTradePartner(target.Identity),
                IsTutor = IsTutor(target.Identity),
                TargetIdentity = Identity
            });
        }

        #endregion

        #region Trade

        public Trade Trade { get; set; }

        #endregion

        #region Trade Partner

        private readonly ConcurrentDictionary<uint, TradePartner> m_tradePartners = new();

        public void AddTradePartner(TradePartner partner)
        {
            m_tradePartners.TryAdd(partner.Identity, partner);
        }

        public void RemoveTradePartner(uint idTarget)
        {
            if (m_tradePartners.ContainsKey(idTarget))
                m_tradePartners.TryRemove(idTarget, out _);
        }

        public async Task<bool> CreateTradePartnerAsync(Character target)
        {
            if (IsTradePartner(target.Identity) || target.IsTradePartner(Identity))
            {
                await SendAsync(Language.StrTradeBuddyAlreadyAdded);
                return false;
            }

            var business = new DbBusiness
            {
                User = GetDatabaseObject(),
                Business = target.GetDatabaseObject(),
                Date = DateTime.Now.AddDays(3)
            };

            if (!await ServerDbContext.SaveAsync(business))
            {
                await SendAsync(Language.StrTradeBuddySomethingWrong);
                return false;
            }

            TradePartner me;
            TradePartner targetTp;
            AddTradePartner(me = new TradePartner(this, business));
            target.AddTradePartner(targetTp = new TradePartner(target, business));

            await me.SendAsync();
            await targetTp.SendAsync();

            await BroadcastRoomMsgAsync(string.Format(Language.StrTradeBuddyAnnouncePartnership, Name, target.Name));
            return true;
        }

        public async Task<bool> DeleteTradePartnerAsync(uint idTarget)
        {
            if (!IsTradePartner(idTarget))
                return false;

            TradePartner partner = GetTradePartner(idTarget);
            if (partner == null)
                return false;

            await partner.SendRemoveAsync();
            RemoveTradePartner(idTarget);
            await SendAsync(string.Format(Language.StrTradeBuddyBrokePartnership1, partner.Name));

            Task<bool> delete = partner.DeleteAsync();
            Character target = RoleManager.GetUser(idTarget);
            if (target != null)
            {
                partner = target.GetTradePartner(Identity);
                if (partner != null)
                {
                    await partner.SendRemoveAsync();
                    target.RemoveTradePartner(Identity);
                }

                await target.SendAsync(string.Format(Language.StrTradeBuddyBrokePartnership0, Name));
            }

            await delete;
            return true;
        }

        public async Task LoadTradePartnerAsync()
        {
            List<DbBusiness> tps = await BusinessRepository.GetAsync(Identity);
            foreach (DbBusiness tp in tps)
            {
                var db = new TradePartner(this, tp);
                AddTradePartner(db);
                await db.SendAsync();
            }
        }

        public TradePartner GetTradePartner(uint target)
        {
            return m_tradePartners.TryGetValue(target, out TradePartner result) ? result : null;
        }

        public bool IsTradePartner(uint target)
        {
            return m_tradePartners.ContainsKey(target);
        }

        public bool IsValidTradePartner(uint target)
        {
            return m_tradePartners.ContainsKey(target) && m_tradePartners[target].IsValid();
        }

        #endregion

        #region Equipment

        public Item Headgear => UserPackage[Item.ItemPosition.Headwear];
        public Item Necklace => UserPackage[Item.ItemPosition.Necklace];
        public Item Ring => UserPackage[Item.ItemPosition.Ring];
        public Item RightHand => UserPackage[Item.ItemPosition.RightHand];
        public Item LeftHand => UserPackage[Item.ItemPosition.LeftHand];
        public Item Armor => UserPackage[Item.ItemPosition.Armor];
        public Item Boots => UserPackage[Item.ItemPosition.Boots];
        public Item Garment => UserPackage[Item.ItemPosition.Garment];
        public Item Mount => UserPackage[Item.ItemPosition.Steed];

        #endregion

        #region User Package

        public uint LastAddItemIdentity { get; set; }

        public UserPackage UserPackage { get; }

        public async Task<bool> SpendEquipItemAsync(uint dwItem, uint dwAmount, bool bSynchro)
        {
            if (dwItem <= 0)
                return false;

            Item item = null;
            if (UserPackage[Item.ItemPosition.RightHand]?.GetItemSubType() == dwItem &&
                UserPackage[Item.ItemPosition.RightHand]?.Durability >= dwAmount)
                item = UserPackage[Item.ItemPosition.RightHand];
            else if (UserPackage[Item.ItemPosition.LeftHand]?.GetItemSubType() == dwItem)
                item = UserPackage[Item.ItemPosition.LeftHand];

            if (item == null)
                return false;

            if (!item.IsExpend() && item.Durability < dwAmount && !item.IsArrowSort())
                return false;

            if (item.IsExpend())
            {
                item.Durability = (ushort) Math.Max(0, item.Durability - (int) dwAmount);
                if (bSynchro)
                    await SendAsync(new MsgItemInfo(item, MsgItemInfo<Client>.ItemMode.Update));
            }
            else
            {
                if (item.IsNonsuchItem())
                    await Log.GmLogAsync("SpendEquipItem",
                                         $"{Name}({Identity}) Spend item:[id={item.Identity}, type={item.Type}], dur={item.Durability}, max_dur={item.MaximumDurability}");
            }

            if (item.IsArrowSort() && item.Durability == 0)
            {
                Item.ItemPosition pos = item.Position;
                await UserPackage.UnEquipAsync(item.Position, UserPackage.RemovalType.Delete);
                Item other = UserPackage.GetItemByType(item.Type);
                if (other != null)
                    await UserPackage.EquipItemAsync(other, pos);
            }

            if (item.Durability > 0)
                await item.SaveAsync();
            return true;
        }

        public async Task<bool> DecEquipmentDurabilityAsync(bool beingAttacked, int hitByMagic, ushort useItemNum)
        {
            int nInc = -1 * useItemNum;

            for (var i = Item.ItemPosition.Headwear; i <= Item.ItemPosition.Crop; i++)
            {
                if (i == Item.ItemPosition.Garment || i == Item.ItemPosition.Gourd || i == Item.ItemPosition.Steed
                    || i == Item.ItemPosition.SteedArmor || i == Item.ItemPosition.LeftHandAccessory ||
                    i == Item.ItemPosition.RightHandAccessory)
                    continue;
                if (hitByMagic == 1)
                {
                    if (i == Item.ItemPosition.Ring
                        || i == Item.ItemPosition.RightHand
                        || i == Item.ItemPosition.LeftHand
                        || i == Item.ItemPosition.Boots)
                    {
                        if (!beingAttacked)
                            await AddEquipmentDurabilityAsync(i, nInc);
                    }
                    else
                    {
                        if (beingAttacked)
                            await AddEquipmentDurabilityAsync(i, nInc);
                    }
                }
                else
                {
                    if (i == Item.ItemPosition.Ring
                        || i == Item.ItemPosition.RightHand
                        || i == Item.ItemPosition.LeftHand
                        || i == Item.ItemPosition.Boots)
                    {
                        if (!beingAttacked)
                            await AddEquipmentDurabilityAsync(i, -1);
                    }
                    else
                    {
                        if (beingAttacked)
                            await AddEquipmentDurabilityAsync(i, nInc);
                    }
                }
            }

            return true;
        }

        public async Task AddEquipmentDurabilityAsync(Item.ItemPosition pos, int nInc)
        {
            if (nInc >= 0)
                return;

            Item item = UserPackage[pos];
            if (item == null
                || !item.IsEquipment()
                || item.GetItemSubType() == 2100)
                return;

            ushort nOldDur = item.Durability;
            var nDurability = (ushort) Math.Max(0, item.Durability + nInc);

            if (nDurability < 100)
            {
                if (nDurability % 10 == 0)
                    await SendAsync(string.Format(Language.StrDamagedRepair, item.Itemtype.Name));
            }
            else if (nDurability < 200)
            {
                if (nDurability % 10 == 0)
                    await SendAsync(string.Format(Language.StrDurabilityRepair, item.Itemtype.Name));
            }

            item.Durability = nDurability;
            await item.SaveAsync();

            var noldDur = (int) Math.Floor(nOldDur / 100f);
            var nnewDur = (int) Math.Floor(nDurability / 100f);

            if (nDurability <= 0)
                await SendAsync(new MsgItemInfo(item, MsgItemInfo<Client>.ItemMode.Update));
            else if (noldDur != nnewDur) await SendAsync(new MsgItemInfo(item, MsgItemInfo<Client>.ItemMode.Update));
        }

        public bool CheckWeaponSubType(uint idItem, uint dwNum = 0)
        {
            var items = new uint[idItem.ToString().Length / 3];
            for (var i = 0; i < items.Length; i++)
                if (idItem > 999 && idItem != 40000 && idItem != 50000)
                {
                    int idx = i * 3; // + (i > 0 ? -1 : 0);
                    items[i] = uint.Parse(idItem.ToString().Substring(idx, 3));
                }
                else
                {
                    items[i] = uint.Parse(idItem.ToString());
                }

            if (items.Length <= 0) return false;

            foreach (uint dwItem in items)
            {
                if (dwItem <= 0) continue;

                if (UserPackage[Item.ItemPosition.RightHand] != null &&
                    UserPackage[Item.ItemPosition.RightHand].GetItemSubType() == dwItem &&
                    UserPackage[Item.ItemPosition.RightHand].Durability >= dwNum)
                    return true;
                if (UserPackage[Item.ItemPosition.LeftHand] != null &&
                    UserPackage[Item.ItemPosition.LeftHand].GetItemSubType() == dwItem &&
                    UserPackage[Item.ItemPosition.LeftHand].Durability >= dwNum)
                    return true;

                ushort[] set1Hand = {410, 420, 421, 430, 440, 450, 460, 480, 481, 490};
                ushort[] set2Hand = {510, 530, 540, 560, 561, 580};
                ushort[] setSword = {420, 421};
                ushort[] setSpecial = {601, 610, 611, 612, 613};

                if (dwItem == 40000 || dwItem == 400)
                    if (UserPackage[Item.ItemPosition.RightHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        for (var i = 0; i < set1Hand.Length; i++)
                            if (item.GetItemSubType() == set1Hand[i] && item.Durability >= dwNum)
                                return true;
                    }

                if (dwItem == 50000)
                    if (UserPackage[Item.ItemPosition.RightHand] != null)
                    {
                        if (dwItem == 50000) return true;

                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        for (var i = 0; i < set2Hand.Length; i++)
                            if (item.GetItemSubType() == set2Hand[i] && item.Durability >= dwNum)
                                return true;
                    }

                if (dwItem == 50) // arrow
                    if (UserPackage[Item.ItemPosition.RightHand] != null &&
                        UserPackage[Item.ItemPosition.LeftHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        Item arrow = UserPackage[Item.ItemPosition.LeftHand];
                        if (arrow.GetItemSubType() == 1050 && arrow.Durability >= dwNum)
                            return true;
                    }

                if (dwItem == 500)
                    if (UserPackage[Item.ItemPosition.RightHand] != null &&
                        UserPackage[Item.ItemPosition.LeftHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        if (item.GetItemSubType() == idItem && item.Durability >= dwNum)
                            return true;
                    }

                if (dwItem == 420)
                    if (UserPackage[Item.ItemPosition.RightHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        for (var i = 0; i < setSword.Length; i++)
                            if (item.GetItemSubType() == setSword[i] && item.Durability >= dwNum)
                                return true;
                    }

                if (dwItem == 601 || dwItem == 610 || dwItem == 611 || dwItem == 612 || dwItem == 613)
                    if (UserPackage[Item.ItemPosition.RightHand] != null)
                    {
                        Item item = UserPackage[Item.ItemPosition.RightHand];
                        if (item.GetItemSubType() == dwItem && item.Durability >= dwNum)
                            return true;
                    }
            }

            return false;
        }

        #endregion

        #region Booth

        public BoothNpc Booth { get; private set; }

        public async Task<bool> CreateBoothAsync()
        {
            if (Booth != null)
            {
                await Booth.LeaveMapAsync();
                Booth = null;
                return false;
            }

            if (Map?.IsBoothEnable() != true)
            {
                await SendAsync(Language.StrBoothRegionCantSetup);
                return false;
            }

            Booth = new BoothNpc(this);
            if (!await Booth.InitializeAsync())
                return false;
            return true;
        }

        public async Task<bool> DestroyBoothAsync()
        {
            if (Booth == null)
                return false;

            await Booth.LeaveMapAsync();
            Booth = null;
            return true;
        }

        public bool AddBoothItem(uint idItem, uint value, MsgItem<Client>.Moneytype type)
        {
            if (Booth == null)
                return false;

            if (!Booth.ValidateItem(idItem))
                return false;

            Item item = UserPackage[idItem];
            return Booth.AddItem(item, value, type);
        }

        public bool RemoveBoothItem(uint idItem)
        {
            if (Booth == null)
                return false;
            return Booth.RemoveItem(idItem);
        }

        public async Task<bool> SellBoothItemAsync(uint idItem, Character target)
        {
            if (Booth == null)
                return false;

            if (target.Identity == Identity)
                return false;

            if (!target.UserPackage.IsPackSpare(1))
                return false;

            if (GetDistance(target) > Screen.VIEW_SIZE)
                return false;

            if (!Booth.ValidateItem(idItem))
                return false;

            BoothItem item = Booth.QueryItem(idItem);
            var value = (int) item.Value;
            string moneyType = item.IsSilver ? Language.StrSilvers : Language.StrConquerPoints;
            if (item.IsSilver)
            {
                if (!await target.SpendMoneyAsync((int) item.Value, true))
                    return false;
                await AwardMoneyAsync(value);
            }
            else
            {
                if (!await target.SpendConquerPointsAsync((int) item.Value, true))
                    return false;
                await AwardConquerPointsAsync(value);
            }

            Booth.RemoveItem(idItem);

            await BroadcastRoomMsgAsync(new MsgItem(item.Identity, MsgItem<Client>.ItemActionType.BoothRemove)
                                {Command = Booth.Identity}, true);
            await UserPackage.RemoveFromInventoryAsync(item.Item, UserPackage.RemovalType.RemoveAndDisappear);
            await item.Item.ChangeOwnerAsync(target.Identity, Item.ChangeOwnerType.BoothSale);
            await target.UserPackage.AddItemAsync(item.Item);

            await SendAsync(string.Format(Language.StrBoothSold, target.Name, item.Item.Name, value, moneyType),
                            TalkChannel.Talk, Color.White);
            await target.SendAsync(string.Format(Language.StrBoothBought, item.Item.Name, value, moneyType),
                                   TalkChannel.Talk, Color.White);

            DbTrade trade = new()
            {
                Type = DbTrade.TradeType.Booth,
                UserIpAddress = Client.IpAddress,
                UserMacAddress = Client.MacAddress,
                TargetIpAddress = target.Client.IpAddress,
                TargetMacAddress = target.Client.MacAddress,
                MapIdentity = MapIdentity,
                TargetEmoney = item.IsSilver ? 0 : item.Value,
                TargetMoney = item.IsSilver ? item.Value : 0,
                UserEmoney = 0,
                UserMoney = 0,
                TargetIdentity = target.Identity,
                UserIdentity = Identity,
                TargetX = target.MapX,
                TargetY = target.MapY,
                UserX = MapX,
                UserY = MapY,
                Timestamp = DateTime.Now
            };

            if (!await ServerDbContext.SaveAsync(trade))
            {
                await Log.GmLogAsync("booth_sale", $"{item.Item.Identity},{item.Item.PlayerIdentity},{Identity},{item.Item.Type},{item.IsSilver},{item.Value},{item.Item.ToJson()}");
                return true;
            }

            DbTradeItem tradeItem = new()
            {
                TradeIdentity = trade.Identity,
                SenderIdentity = Identity,
                ItemIdentity = item.Identity,
                Itemtype = item.Item.Type,
                Chksum = (uint)item.Item.ToJson().GetHashCode(),
                JsonData = item.Item.ToJson()
            };
            await ServerDbContext.SaveAsync(tradeItem);
            return true;
        }

        #endregion

        #region Map Item

        public async Task<bool> DropItemAsync(uint idItem, int x, int y)
        {
            var pos = new Point(x, y);
            if (!Map.FindDropItemCell(9, ref pos))
                return false;

            Item item = UserPackage.FindByIdentity(idItem);
            if (item == null)
                return false;

            if (Booth?.QueryItem(idItem) != null)
                return false;

            if (Trade != null)
                return false;

            if (item.IsSuspicious())
                return false;

            await Log.GmLogAsync("drop_item", $"{Name}({Identity}) drop item:[id={item.Identity}, type={item.Type}], dur={item.Durability}, max_dur={item.OriginalMaximumDurability}\r\n\t{item.ToJson()}");

            var dropItemLog = new DbItemDrop
            {
                UserId = Identity,
                ItemId = item.Identity,
                ItemType = item.Type,
                MapId = MapIdentity,
                X = MapX,
                Y = MapY,
                Addition = item.Plus,
                Gem1 = (byte) item.SocketOne,
                Gem2 = (byte) item.SocketTwo,
                ReduceDmg = item.ReduceDamage,
                AddLife = item.Enchantment,
                Data = item.Data,
                DropTime = UnixTimestamp.Now()
            };
            await ServerDbContext.SaveAsync(dropItemLog);

            if (item.CanBeDropped() && item.IsDisappearWhenDropped())
            {
                return await UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.Delete);
            }
            if (item.CanBeDropped())
            {
                await UserPackage.RemoveFromInventoryAsync(item, UserPackage.RemovalType.RemoveAndDisappear);
            }
            else
            {
                await SendAsync(string.Format(Language.StrItemCannotDiscard, item.Name));
                return false;
            }

            item.Position = Item.ItemPosition.Floor;
            await item.SaveAsync();

            var mapItem = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity, dropItemLog);
            if (await mapItem.CreateAsync(Map, pos, item, Identity))
            {
                await mapItem.EnterMapAsync();
                await item.SaveAsync();
            }
            else
            {
                IdentityGenerator.MapItem.ReturnIdentity(mapItem.Identity);
                if (IsGm()) await SendAsync("The MapItem object could not be created. Check Output log");
                return false;
            }

            return true;
        }

        public async Task<bool> DropSilverAsync(uint amount)
        {
            if (amount > 10000000)
                return false;

            if (Trade != null)
                return false;

            var pos = new Point(MapX, MapY);
            if (!Map.FindDropItemCell(1, ref pos))
                return false;

            if (!await SpendMoneyAsync((int) amount, true))
                return false;

            await Log.GmLogAsync("drop_money", $"drop money: {Identity} {Name} has dropped {amount} silvers");

            var dropItemLog = new DbItemDrop
            {
                UserId = Identity,
                ItemId = 0,
                ItemType = amount,
                MapId = MapIdentity,
                X = MapX,
                Y = MapY,
                Addition = 0,
                Gem1 = 0,
                Gem2 = 0,
                ReduceDmg = 0,
                AddLife = 0,
                Data = 0,
                DropTime = UnixTimestamp.Now()
            };
            await ServerDbContext.SaveAsync(dropItemLog);

            var mapItem = new MapItem((uint) IdentityGenerator.MapItem.GetNextIdentity, dropItemLog);
            if (await mapItem.CreateMoneyAsync(Map, pos, amount, 0u, MapItem.DropMode.Common))
            {
                await mapItem.EnterMapAsync();
            }
            else
            {
                IdentityGenerator.MapItem.ReturnIdentity(mapItem.Identity);
                if (IsGm()) await SendAsync("The DropSilver MapItem object could not be created. Check Output log");
                return false;
            }

            return true;
        }

        public async Task<bool> PickMapItemAsync(uint idItem)
        {
            var mapItem = Map.QueryAroundRole(this, idItem) as MapItem;
            if (mapItem == null)
                return false;

            if (GetDistance(mapItem) > 0)
            {
                await SendAsync(Language.StrTargetNotInRange);
                return false;
            }

            if (!mapItem.IsMoney() && !UserPackage.IsPackSpare(1))
            {
                await SendAsync(Language.StrYourBagIsFull);
                return false;
            }

            if ((mapItem.Mode.HasFlag(MapItem.DropMode.OnlyOwner) || mapItem.Mode.HasFlag(MapItem.DropMode.Bound))
                && mapItem.OwnerIdentity != Identity)
            {
                await SendAsync(Language.StrCannotPickupOtherItems);
                return false;
            }

            if (mapItem.OwnerIdentity != Identity && mapItem.IsPrivate())
            {
                Character owner = RoleManager.GetUser(mapItem.OwnerIdentity);
                if (owner != null && !IsMate(owner))
                    if (Team == null || !Team.IsMember(mapItem.OwnerIdentity) ||
                        mapItem.IsMoney() && !Team.MoneyEnable || mapItem.IsJewel() && !Team.JewelEnable ||
                        mapItem.IsItem() && !Team.ItemEnable)
                    {
                        await SendAsync(Language.StrCannotPickupOtherItems);
                        return false;
                    }
            }

            if (mapItem.IsMoney())
            {
                await AwardMoneyAsync((int) mapItem.Money);
                if (mapItem.Money > 1000)
                    await SendAsync(new MsgAction
                    {
                        Identity = Identity,
                        Command = mapItem.Money,
                        ArgumentX = MapX,
                        ArgumentY = MapY,
                        Action = MsgAction<Client>.ActionType.MapGold
                    });
                await SendAsync(string.Format(Language.StrPickupSilvers, mapItem.Money));

                await Log.GmLogAsync("pickup_money",
                                     $"User[{Identity},{Name}] picked up {mapItem.Money} at {MapIdentity}({Map.Name}) {MapX}, {MapY}");
            }
            else
            {
                Item item = await mapItem.GetInfoAsync(this);

                if (item != null)
                {
                    await UserPackage.AddItemAsync(item);
                    await SendAsync(string.Format(Language.StrPickupItem, item.Name));

                    await Log.GmLogAsync("pickup_item",
                                         $"User[{Identity},{Name}] picked up (id:{mapItem.ItemIdentity}) {mapItem.Itemtype} at {MapIdentity}({Map.Name}) {MapX}, {MapY}");

                    if (VipLevel > 0 && mapItem.IsConquerPointsPack())
                        await UserPackage.UseItemAsync(item.Identity, Item.ItemPosition.Inventory);

                    if (VipLevel > 1 && UserPackage.MultiCheckItem(Item.TYPE_METEOR, Item.TYPE_METEOR, 10, true))
                    {
                        await UserPackage.MultiSpendItemAsync(Item.TYPE_METEOR, Item.TYPE_METEOR, 10, true);
                        await UserPackage.AwardItemAsync(Item.TYPE_METEOR_SCROLL);
                    }

                    if (VipLevel > 3 &&
                        UserPackage.MultiCheckItem(Item.TYPE_DRAGONBALL, Item.TYPE_DRAGONBALL, 10, true))
                    {
                        await UserPackage.MultiSpendItemAsync(Item.TYPE_DRAGONBALL, Item.TYPE_DRAGONBALL, 10, true);
                        await UserPackage.AwardItemAsync(Item.TYPE_DRAGONBALL_SCROLL);
                    }
                }
            }

            var itemPickUpLog = new DbItemPickUp
            {
                PickUpTime = UnixTimestamp.Now(),
                UserId = Identity,
                ItemId = mapItem.IsMoney() ? 0 : mapItem.ItemIdentity,
                ItemType = mapItem.IsMoney() ? mapItem.Money : mapItem.Itemtype
            };
            if (mapItem.ItemDrop != null)
            {
                itemPickUpLog.DropId = mapItem.ItemDrop.Id;
            }
            await ServerDbContext.SaveAsync(itemPickUpLog);

            await mapItem.LeaveMapAsync();
            return true;
        }

        #endregion

        #region Team

        public uint VirtuePoints
        {
            get => mDbObject.Virtue;
            set => mDbObject.Virtue = value;
        }

        public Team Team { get; set; }

        #endregion

        #region Weapon Skill

        public WeaponSkill WeaponSkill { get; }

        public async Task AddWeaponSkillExpAsync(ushort usType, int nExp, bool byAction = false)
        {
            DbWeaponSkill skill = WeaponSkill[usType];
            if (skill == null)
            {
                await WeaponSkill.CreateAsync(usType, 0);
                if ((skill = WeaponSkill[usType]) == null)
                    return;
            }

            if (skill.Level >= MAX_WEAPONSKILLLEVEL)
                return;

            if (skill.Unlearn != 0)
                skill.Unlearn = 0;

            nExp = (int) (nExp * (1 + VioletGemBonus / 100d));

            uint nIncreaseLev = 0;
            if (skill.Level > MASTER_WEAPONSKILLLEVEL)
            {
                int nRatio = 100 - (skill.Level - MASTER_WEAPONSKILLLEVEL) * 20;
                if (nRatio < 10)
                    nRatio = 10;
                nExp = Calculations.MulDiv(nExp, nRatio, 100) / 2;
            }

            var nNewExp = (int) Math.Max(nExp + skill.Experience, skill.Experience);

#if DEBUG
            if (IsPm())
                await SendAsync($"Add Weapon Skill exp: {nExp}, CurExp: {nNewExp}");
#endif

            int nLevel = skill.Level;
            var oldPercent = (uint) (skill.Experience / (double) MsgWeaponSkill.RequiredExperience[nLevel] * 100);
            if (nLevel < MAX_WEAPONSKILLLEVEL)
                if (nNewExp > MsgWeaponSkill.RequiredExperience[nLevel] ||
                    nLevel >= skill.OldLevel / 2 && nLevel < skill.OldLevel)
                {
                    nNewExp = 0;
                    nIncreaseLev = 1;
                }

            if (byAction || skill.Level < Level / 10 + 1
                         || skill.Level >= MASTER_WEAPONSKILLLEVEL)
            {
                skill.Experience = (uint) nNewExp;

                if (nIncreaseLev > 0)
                {
                    skill.Level += (byte) nIncreaseLev;
                    await SendAsync(new MsgWeaponSkill
                    {
                        Experience = skill.Experience,
                        Level = skill.Level,
                        Identity = skill.Type
                    });
                    await SendAsync(Language.StrWeaponSkillUp);
                    await WeaponSkill.SaveAsync(skill);
                }
                else
                {
                    await SendAsync(new MsgFlushExp
                    {
                        Action = MsgFlushExp<Client>.FlushMode.WeaponSkill,
                        Identity = (ushort) skill.Type,
                        Experience = skill.Experience
                    });

                    var newPercent =
                        (int) (skill.Experience / (double) MsgWeaponSkill.RequiredExperience[nLevel] * 100);
                    if (oldPercent - oldPercent % 10 != newPercent - newPercent % 10)
                        await WeaponSkill.SaveAsync(skill);
                }
            }
        }

        #endregion

        #region Syndicate

        public Syndicate Syndicate { get; set; }
        public SyndicateMember SyndicateMember => Syndicate?.QueryMember(Identity);
        public ushort SyndicateIdentity => Syndicate?.Identity ?? 0;
        public string SyndicateName => Syndicate?.Name ?? Language.StrNone;

        public SyndicateMember.SyndicateRank SyndicateRank =>
            SyndicateMember?.Rank ?? SyndicateMember.SyndicateRank.None;

        public string SyndicateRankName => SyndicateMember?.RankName ?? Language.StrNone;

        public async Task<bool> CreateSyndicateAsync(string name, int price = 1000000)
        {
            if (Syndicate != null)
            {
                await SendAsync(Language.StrSynAlreadyJoined);
                return false;
            }

            if (name.Length > 15) return false;

            if (!Kernel.IsValidName(name))
                return false;

            if (SyndicateManager.GetSyndicate(name) != null)
            {
                await SendAsync(Language.StrSynNameInUse);
                return false;
            }

            if (!await SpendMoneyAsync(price))
            {
                await SendAsync(Language.StrNotEnoughMoney);
                return false;
            }

            Syndicate = new Syndicate();
            if (!await Syndicate.CreateAsync(name, price, this))
            {
                Syndicate = null;
                await AwardMoneyAsync(price);
                return false;
            }

            if (!SyndicateManager.AddSyndicate(Syndicate))
            {
                await Syndicate.DeleteAsync();
                Syndicate = null;
                await AwardMoneyAsync(price);
                return false;
            }

            await RoleManager.BroadcastMsgAsync(string.Format(Language.StrSynCreate, Name, name), TalkChannel.Talk,
                                                Color.White);
            await SendSyndicateAsync();
            await Screen.SynchroScreenAsync();
            await Syndicate.BroadcastNameAsync();
            return true;
        }

        public async Task<bool> DisbandSyndicateAsync()
        {
            if (SyndicateIdentity == 0)
                return false;

            if (Syndicate.Leader.UserIdentity != Identity)
                return false;

            if (Syndicate.MemberCount > 1)
            {
                await SendAsync(Language.StrSynNoDisband);
                return false;
            }

            return await Syndicate.DisbandAsync(this);
        }

        public async Task SendSyndicateAsync()
        {
            if (Syndicate != null)
            {
                await SendAsync(new MsgSyndicateAttributeInfo
                {
                    Identity = SyndicateIdentity,
                    Rank = (int) SyndicateRank,
                    MemberAmount = Syndicate.MemberCount,
                    Funds = Syndicate.Money,
                    PlayerDonation = SyndicateMember.Silvers,
                    LeaderName = Syndicate.Leader.UserName,
                    ConditionLevel = Syndicate.LevelRequirement,
                    ConditionMetempsychosis = Syndicate.MetempsychosisRequirement,
                    ConditionProfession = (int) Syndicate.ProfessionRequirement,
                    ConquerPointsFunds = Syndicate.ConquerPoints,
                    PositionExpiration = uint.Parse(SyndicateMember.PositionExpiration?.ToString("yyyyMMdd") ?? "0"),
                    EnrollmentDate = uint.Parse(SyndicateMember.JoinDate.ToString("yyyyMMdd")),
                    Level = Syndicate.Level
                });
                await SendAsync(new MsgSyndicate
                {
                    Mode = MsgSyndicate<Client>.SyndicateRequest.Bulletin,
                    Strings = new List<string> {Syndicate.Announce},
                    Identity = uint.Parse(Syndicate.AnnounceDate.ToString("yyyyMMdd"))
                });
                await Syndicate.SendAsync(this);
                await SendAsync(new MsgSynpOffer(SyndicateMember));
                await SynchroAttributesAsync(ClientUpdateType.TotemPoleBattlePower,
                                             (ulong) Syndicate.TotemSharedBattlePower, true);
            }
            else
            {
                await SendAsync(new MsgSyndicateAttributeInfo
                {
                    Rank = (int) SyndicateMember.SyndicateRank.None
                });
            }
        }

        #endregion

        #region Family

        public Family Family { get; set; }
        public FamilyMember FamilyMember => Family?.GetMember(Identity);

        public uint FamilyIdentity => Family?.Identity ?? 0;
        public string FamilyName => Family?.Name ?? Language.StrNone;

        public Family.FamilyRank FamilyPosition => FamilyMember?.Rank ?? Family.FamilyRank.None;

        public async Task LoadFamilyAsync()
        {
            Family = FamilyManager.FindByUser(Identity);
            if (Family == null)
            {
                if (MateIdentity != 0)
                {
                    Family family = FamilyManager.FindByUser(MateIdentity);
                    FamilyMember mateFamily = family?.GetMember(MateIdentity);
                    if (mateFamily == null || mateFamily.Rank == Family.FamilyRank.Spouse)
                        return;

                    if (!await family.AppendMemberAsync(null, this, Family.FamilyRank.Spouse))
                        return;
                }
            }
            else
            {
                await SendFamilyAsync();
                await Family.SendRelationsAsync(this);
            }

            if (Family == null)
                return;

            var war = EventManager.GetEvent<FamilyWar>();
            if (war == null)
                return;

            if (Family.ChallengeMap == 0)
                return;

            GameMap map = MapManager.GetMap(Family.ChallengeMap);
            if (map == null)
                return;

            await SendAsync(string.Format(Language.StrPrepareToChallengeFamilyLogin, map.Name), TalkChannel.Talk,
                            Color.White);

            map = MapManager.GetMap(Family.FamilyMap);
            if (map == null)
                return;

            if (war.GetChallengersByMap(map.Identity).Count == 0)
                return;

            await SendAsync(string.Format(Language.StrPrepareToDefendFamilyLogin, map.Name), TalkChannel.Talk,
                            Color.White);
        }

        private string FamilyOccupyString
        {
            get
            {
                var war = EventManager.GetEvent<FamilyWar>();
                if (war == null || Family == null)
                    return "0 0 0 0 0 0 0 0";
                uint idNpc = war.GetDominatingNpc(Family)?.Identity ?? 0;
                return "0 " +
                       $"{Family.OccupyDays} " +
                       $"{war.GetNextReward(this, idNpc)} " +
                       $"{war.GetNextWeekReward(this, idNpc)} " +
                       $"{(war.IsChallenged(Family.FamilyMap) ? 1 : 0)} " +
                       $"{(war.HasRewardToClaim(this) ? 1 : 0)} " +
                       $"{(war.HasExpToClaim(this) ? 1 : 0)}";
            }
        }

        public string FamilyDominatedMap =>
            Family != null ? EventManager.GetEvent<FamilyWar>()?.GetMap(Family.FamilyMap)?.Name ?? "" : "";

        public string FamilyChallengedMap => Family != null
                                                 ? EventManager.GetEvent<FamilyWar>()?.GetMap(Family.ChallengeMap)
                                                               ?.Name ?? ""
                                                 : "";

        public Task SendFamilyAsync()
        {
            if (Family == null)
                return Task.CompletedTask;

            var msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily<Client>.FamilyAction.Query
            };
            msg.Strings.Add(
                $"{Family.Identity} {Family.MembersCount} {Family.MembersCount} {Family.Money} {Family.Rank} {(int) FamilyPosition} 0 {Family.BattlePowerTower} 0 0 1 {FamilyMember.Proffer}");
            msg.Strings.Add(FamilyName);
            msg.Strings.Add(Name);
            msg.Strings.Add(FamilyOccupyString);
            msg.Strings.Add(FamilyDominatedMap);
            msg.Strings.Add(FamilyChallengedMap);
            return SendAsync(msg);
        }

        public Task SendFamilyOccupyAsync()
        {
            if (Family == null)
                return Task.CompletedTask;

            var msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily<Client>.FamilyAction.QueryOccupy
            };
            // uid occupydays reward nextreward challenged rewardtoclaim exptoclaim
            msg.Strings.Add(FamilyOccupyString);
            return SendAsync(msg);
        }

        public async Task SendNoFamilyAsync()
        {
            var msg = new MsgFamily
            {
                Identity = FamilyIdentity,
                Action = MsgFamily<Client>.FamilyAction.Query
            };
            msg.Strings.Add(FamilyOccupyString);
            msg.Strings.Add("");
            msg.Strings.Add(Name);
            await SendAsync(msg);

            msg.Action = MsgFamily<Client>.FamilyAction.Quit;
            await SendAsync(msg);
        }

        public async Task<bool> CreateFamilyAsync(string name, uint proffer)
        {
            if (Family != null)
                return false;

            if (!Kernel.IsValidName(name))
                return false;

            if (name.Length > 15)
                return false;

            if (FamilyManager.GetFamily(name) != null)
                return false;

            if (!await SpendMoneyAsync((int) proffer, true))
                return false;

            Family = await Family.CreateAsync(this, name, proffer / 2);
            if (Family == null)
                return false;

            await SendFamilyAsync();
            await Family.SendRelationsAsync(this);
            return true;
        }

        public async Task<bool> DisbandFamilyAsync()
        {
            if (Family == null)
                return false;

            if (FamilyPosition != Family.FamilyRank.ClanLeader)
                return false;

            if (Family.MembersCount > 1)
                return false;

            await FamilyMember.DeleteAsync();
            await Family.SoftDeleteAsync();

            Family = null;

            await SendNoFamilyAsync();
            return true;
        }

        public Task SynchroFamilyBattlePowerAsync()
        {
            if (Team == null || Family == null)
                return Task.CompletedTask;

            int bp = Team.FamilyBattlePower(this, out uint provider);
            var msg = new MsgUserAttrib(Identity, ClientUpdateType.FamilySharedBattlePower, provider);
            msg.Append(ClientUpdateType.FamilySharedBattlePower, (ulong) bp);
            return SendAsync(msg);
        }

        public int FamilyBattlePower => Team?.FamilyBattlePower(this, out _) ?? 0;

        #endregion

        #region Friend

        private readonly ConcurrentDictionary<uint, Friend> mFriends = new();

        public int FriendAmount => mFriends.Count;

        public int MaxFriendAmount => 50;

        public bool AddFriend(Friend friend)
        {
            return mFriends.TryAdd(friend.Identity, friend);
        }

        public async Task<bool> CreateFriendAsync(Character target)
        {
            if (IsFriend(target.Identity))
                return false;

            var friend = new Friend(this);
            if (!friend.Create(target))
                return false;

            var targetFriend = new Friend(target);
            if (!targetFriend.Create(this))
                return false;

            await friend.SaveAsync();
            await targetFriend.SaveAsync();
            await friend.SendAsync();
            await targetFriend.SendAsync();

            AddFriend(friend);
            target.AddFriend(targetFriend);

            await BroadcastRoomMsgAsync(string.Format(Language.StrMakeFriend, Name, target.Name));
            return true;
        }

        public bool IsFriend(uint idTarget)
        {
            return mFriends.ContainsKey(idTarget);
        }

        public Friend GetFriend(uint idTarget)
        {
            return mFriends.TryGetValue(idTarget, out Friend friend) ? friend : null;
        }

        public async Task<bool> DeleteFriendAsync(uint idTarget, bool notify = false)
        {
            if (!IsFriend(idTarget) || !mFriends.TryRemove(idTarget, out Friend target))
                return false;

            if (target.Online)
            {
                await target.User.DeleteFriendAsync(Identity);
            }
            else
            {
                DbFriend targetFriend = await FriendRepository.GetAsync(Identity, idTarget);
                await using var ctx = new ServerDbContext();
                ctx.Remove(targetFriend);
                await ctx.SaveChangesAsync();
            }

            await target.DeleteAsync();

            await SendAsync(new MsgFriend
            {
                Identity = target.Identity,
                Name = target.Name,
                Action = MsgFriend<Client>.MsgFriendAction.RemoveFriend,
                Online = target.Online
            });

            if (notify)
                await BroadcastRoomMsgAsync(string.Format(Language.StrBreakFriend, Name, target.Name));
            return true;
        }

        public async Task SendAllFriendAsync()
        {
            foreach (Friend friend in mFriends.Values)
            {
                await friend.SendAsync();
                if (friend.Online)
                    await friend.User.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend<Client>.MsgFriendAction.SetOnlineFriend,
                        Online = true
                    });
            }
        }

        public async Task NotifyOfflineFriendAsync()
        {
            foreach (Friend friend in mFriends.Values)
                if (friend.Online)
                    await friend.User.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend<Client>.MsgFriendAction.SetOfflineFriend,
                        Online = true
                    });
        }

        public async Task SendToFriendsAsync(IPacket msg)
        {
            foreach (Friend friend in mFriends.Values.Where(x => x.Online))
                await friend.User.SendAsync(msg);
        }

        #endregion

        #region Enemy

        private readonly ConcurrentDictionary<uint, Enemy> mEnemies = new();

        public bool AddEnemy(Enemy friend)
        {
            return mEnemies.TryAdd(friend.Identity, friend);
        }

        public async Task<bool> CreateEnemyAsync(Character target)
        {
            if (IsEnemy(target.Identity))
                return false;

            var enemy = new Enemy(this);
            if (!await enemy.CreateAsync(target))
                return false;

            await enemy.SaveAsync();
            await enemy.SendAsync();
            AddEnemy(enemy);
            return true;
        }

        public bool IsEnemy(uint idTarget)
        {
            return mEnemies.ContainsKey(idTarget);
        }

        public Enemy GetEnemy(uint idTarget)
        {
            return mEnemies.TryGetValue(idTarget, out Enemy friend) ? friend : null;
        }

        public async Task<bool> DeleteEnemyAsync(uint idTarget)
        {
            if (!IsFriend(idTarget) || !mEnemies.TryRemove(idTarget, out Enemy target))
                return false;

            await target.DeleteAsync();

            await SendAsync(new MsgFriend
            {
                Identity = target.Identity,
                Name = target.Name,
                Action = MsgFriend<Client>.MsgFriendAction.RemoveEnemy,
                Online = true
            });
            return true;
        }

        public async Task SendAllEnemiesAsync()
        {
            foreach (Enemy enemy in mEnemies.Values) await enemy.SendAsync();

            foreach (DbEnemy enemy in await EnemyRepository.GetOwnEnemyAsync(Identity))
            {
                Character user = RoleManager.GetUser(enemy.UserIdentity);
                if (user != null)
                    await user.SendAsync(new MsgFriend
                    {
                        Identity = Identity,
                        Name = Name,
                        Action = MsgFriend<Client>.MsgFriendAction.SetOnlineEnemy,
                        Online = true
                    });
            }
        }

        #endregion

        #region Statistic

        public UserStatistic Statistic { get; }

        public long Iterator = -1;
        public long[] VarData = new long[MAX_VAR_AMOUNT];
        public string[] VarString = new string[MAX_VAR_AMOUNT];

        #endregion

        #region Task Detail

        public TaskDetail TaskDetail { get; }

        #endregion

        #region Events

        public GameEvent CurrentEvent { get; private set; }

        public async Task<bool> SignInEventAsync(GameEvent e)
        {
            if (!e.IsAllowedToJoin(this)) return false;

            CurrentEvent = e;
            await e.OnEnterAsync(this);
            return true;
        }

        public async Task<bool> SignOutEventAsync()
        {
            if (CurrentEvent != null)
                await CurrentEvent.OnExitAsync(this);

            CurrentEvent = null;
            return true;
        }

        #endregion

        #region Tutor

        private DbTutorAccess m_tutorAccess;

        public ulong MentorExpTime
        {
            get => m_tutorAccess?.Experience ?? 0;
            set
            {
                m_tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                m_tutorAccess.Experience = value;
            }
        }

        public ushort MentorAddLevexp
        {
            get => m_tutorAccess?.Composition ?? 0;
            set
            {
                m_tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                m_tutorAccess.Composition = value;
            }
        }

        public ushort MentorGodTime
        {
            get => m_tutorAccess?.Blessing ?? 0;
            set
            {
                m_tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = Identity
                };
                m_tutorAccess.Blessing = value;
            }
        }

        public Tutor Guide;

        private readonly ConcurrentDictionary<uint, Tutor> m_apprentices = new();

        public Tutor GetStudent(uint idStudent)
        {
            return m_apprentices.TryGetValue(idStudent, out Tutor value) ? value : null;
        }

        public int ApprenticeCount => m_apprentices.Count;

        public async Task LoadGuideAsync()
        {
            DbTutor tutor = await TutorRepository.GetAsync(Identity);
            if (tutor != null)
            {
                Guide = await Tutor.CreateAsync(tutor);
                if (Guide != null)
                {
                    await Guide.SendTutorAsync();
                    await Guide.SendStudentAsync();

                    Character guide = Guide.Guide;
                    if (guide != null)
                    {
                        await SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, (uint) Guide.SharedBattlePower,
                                                     (uint) guide.BattlePower);
                        await guide.SendAsync(string.Format(Language.StrGuideStudentLogin, Name));
                    }
                }
            }

            List<DbTutor> apprentices = await TutorRepository.GetStudentsAsync(Identity);
            foreach (DbTutor dbApprentice in apprentices)
            {
                var apprentice = await Tutor.CreateAsync(dbApprentice);
                if (apprentice != null)
                {
                    m_apprentices.TryAdd(dbApprentice.StudentId, apprentice);
                    await apprentice.SendTutorAsync();
                    await apprentice.SendStudentAsync();

                    Character student = apprentice.Student;
                    if (student != null)
                    {
                        await student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower,
                                                             (uint) apprentice.SharedBattlePower, (uint) BattlePower);
                        await student.SendAsync(string.Format(Language.StrGuideTutorLogin, Name));
                    }
                }
            }

            m_tutorAccess = await TutorAccessRepository.GetAsync(Identity);
        }

        public static async Task<bool> CreateTutorRelationAsync(Character guide, Character apprentice)
        {
            if (guide.Level < apprentice.Level || guide.Metempsychosis < apprentice.Metempsychosis)
                return false;

            int deltaLevel = guide.Level - apprentice.Level;
            if (apprentice.Metempsychosis == 0)
            {
                if (deltaLevel < 30)
                    return false;
            }
            else if (apprentice.Metempsychosis == 1)
            {
                if (deltaLevel > 20)
                    return false;
            }
            else
            {
                if (deltaLevel > 10)
                    return false;
            }

            DbTutorType type = TutorManager.GetTutorType(guide.Level);
            if (type == null || guide.ApprenticeCount >= type.StudentNum)
                return false;

            if (apprentice.Guide != null)
                return false;

            if (guide.m_apprentices.ContainsKey(apprentice.Identity))
                return false;

            var dbTutor = new DbTutor
            {
                GuideId = guide.Identity,
                StudentId = apprentice.Identity,
                Date = DateTime.Now
            };
            if (!await ServerDbContext.SaveAsync(dbTutor))
                return false;

            var tutor = await Tutor.CreateAsync(dbTutor);

            apprentice.Guide = tutor;
            await tutor.SendTutorAsync();
            guide.m_apprentices.TryAdd(apprentice.Identity, tutor);
            await tutor.SendStudentAsync();
            await apprentice.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, (uint) tutor.SharedBattlePower,
                                                    (uint) guide.BattlePower);
            return true;
        }

        public async Task SynchroApprenticesSharedBattlePowerAsync()
        {
            foreach (Tutor apprentice in m_apprentices.Values.Where(x => x.Student != null))
                await apprentice.Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower,
                                                                (uint) apprentice.SharedBattlePower,
                                                                (uint) (apprentice.Guide?.BattlePower ?? 0));
        }

        /// <summary>
        ///     Returns true if the current user is the tutor of the target ID.
        /// </summary>
        public bool IsTutor(uint idApprentice)
        {
            return m_apprentices.ContainsKey(idApprentice);
        }

        public bool IsApprentice(uint idGuide)
        {
            return Guide?.GuideIdentity == idGuide;
        }

        public void RemoveApprentice(uint idApprentice)
        {
            m_apprentices.TryRemove(idApprentice, out _);
        }

        public Task<bool> SaveTutorAccessAsync()
        {
            if (m_tutorAccess != null)
                return ServerDbContext.SaveAsync(m_tutorAccess);
            return Task.FromResult(true);
        }

        #endregion

        #region Online Training

        public uint GodTimeExp
        {
            get => mDbObject.OnlineGodExpTime;
            set => mDbObject.OnlineGodExpTime = value;
        }

        public uint OnlineTrainingExp
        {
            get => mDbObject.BattleGodExpTime;
            set => mDbObject.BattleGodExpTime = value;
        }

        #endregion

        #region User Title

        public enum UserTitles
        {
            None,
            Vip,
            ElitePkChampionHigh = 10
        }

        private readonly ConcurrentDictionary<uint, DbUserTitle> m_userTitles = new();

        public async Task LoadTitlesAsync()
        {
            List<DbUserTitle> titles = await UserTitleRepository.GetAsync(Identity);
            foreach (DbUserTitle title in titles) m_userTitles.TryAdd(title.TitleId, title);
            await SendTitlesAsync();
        }

        public bool HasTitle(UserTitles idTitle)
        {
            return m_userTitles.ContainsKey((uint) idTitle);
        }

        public List<DbUserTitle> GetUserTitles()
        {
            return m_userTitles.Values.Where(x => x.DelTime > DateTime.Now).ToList();
        }

        public byte UserTitle
        {
            get => mDbObject.TitleSelect;
            set => mDbObject.TitleSelect = value;
        }

        public async Task<bool> AddTitleAsync(UserTitles idTitle, DateTime expiration)
        {
            if (expiration < DateTime.Now)
                return false;

            if (HasTitle(idTitle))
            {
                m_userTitles.TryRemove((uint) idTitle, out DbUserTitle old);
                await ServerDbContext.DeleteAsync(old);
            }

            var title = new DbUserTitle
            {
                PlayerId = Identity,
                TitleId = (uint) idTitle,
                DelTime = expiration,
                Status = 0,
                Type = 0
            };
            await ServerDbContext.SaveAsync(title);
            return m_userTitles.TryAdd((uint) idTitle, title);
        }

        public async Task SendTitlesAsync()
        {
            foreach (byte title in GetUserTitles().Select(x => (byte) x.TitleId))
                await SendAsync(new MsgTitle
                {
                    Action = MsgTitle<Client>.TitleAction.Add,
                    Title = title,
                    Identity = Identity
                });
        }

        #endregion

        #region Equipment Detain

        public async Task SendDetainedEquipmentAsync()
        {
            List<DbDetainedItem> items = await DetainedItemRepository.GetFromDischargerAsync(Identity);
            foreach (DbDetainedItem dbDischarged in items)
            {
                if (dbDischarged.ItemIdentity == 0)
                    continue; // item already claimed back

                DbItem dbItem = await ItemRepository.GetByIdAsync(dbDischarged.ItemIdentity);
                if (dbItem == null)
                {
                    await ServerDbContext.DeleteAsync(dbDischarged);
                    continue;
                }

                Item item = new();
                if (!await item.CreateAsync(dbItem))
                    continue;

                await SendAsync(new MsgDetainItemInfo(dbDischarged, item, MsgDetainItemInfo<Client>.Mode.DetainPage));
            }

            if (items.Count > 0)
                await SendAsync(Language.StrHasDetainEquip, TalkChannel.Talk);
        }

        public async Task SendDetainRewardAsync()
        {
            List<DbDetainedItem> items = await DetainedItemRepository.GetFromHunterAsync(Identity);
            foreach (DbDetainedItem dbDetained in items)
            {
                DbItem dbItem = null;
                Item item = null;

                if (dbDetained.ItemIdentity != 0)
                {
                    dbItem = await ItemRepository.GetByIdAsync(dbDetained.ItemIdentity);
                    if (dbItem == null)
                    {
                        await ServerDbContext.DeleteAsync(dbDetained);
                        continue;
                    }

                    item = new Item();
                    if (!await item.CreateAsync(dbItem))
                        continue;
                }

                bool expired = dbDetained.HuntTime + 60 * 60 * 24 * 7 < UnixTimestamp.Now();
                bool notClaimed = dbDetained.ItemIdentity != 0;

                await SendAsync(new MsgDetainItemInfo(dbDetained, item, MsgDetainItemInfo<Client>.Mode.ClaimPage));
                if (!expired && notClaimed)
                {
                    // ? send message? do nothing
                }
                else if (expired && notClaimed)
                {
                    // ? send message, item ready to be claimed
                    if (ItemManager.Confiscator != null)
                        await SendAsync(
                            string.Format(Language.StrHasEquipBonus, dbDetained.TargetName,
                                          ItemManager.Confiscator.Name, ItemManager.Confiscator.MapX,
                                          ItemManager.Confiscator.MapY), TalkChannel.Talk);
                }
                else if (!notClaimed)
                {
                    if (ItemManager.Confiscator != null)
                        await SendAsync(
                            string.Format(Language.StrHasEmoneyBonus, dbDetained.TargetName,
                                          ItemManager.Confiscator.Name, ItemManager.Confiscator.MapX,
                                          ItemManager.Confiscator.MapY), TalkChannel.Talk);

                    // claimed, show CPs reward
                    await SendAsync(new MsgItem
                    {
                        Action = MsgItem<Client>.ItemActionType.RedeemEquipment,
                        Identity = dbDetained.Identity,
                        Command = dbDetained.TargetIdentity,
                        Argument2 = dbDetained.RedeemPrice
                    });
                }
            }

            if (items.Count > 0 && ItemManager.Confiscator != null)
                await SendAsync(
                    string.Format(Language.StrPkBonus, ItemManager.Confiscator.Name, ItemManager.Confiscator.MapX,
                                  ItemManager.Confiscator.MapY), TalkChannel.Talk);
        }

        #endregion

        #region Monster Kills

        private ConcurrentDictionary<uint, DbMonsterKill> m_monsterKills = new();

        public async Task LoadMonsterKillsAsync()
        {
            m_monsterKills =
                new ConcurrentDictionary<uint, DbMonsterKill>(
                    (await MonsterKillRepository.GetAsync(Identity)).ToDictionary(x => x.Monster));
        }

        public Task KillMonsterAsync(uint type)
        {
            if (!m_monsterKills.TryGetValue(type, out DbMonsterKill value))
                m_monsterKills.TryAdd(type, value = new DbMonsterKill
                {
                    CreatedAt = DateTime.Now,
                    UserIdentity = Identity,
                    Monster = type
                });

            value.Amount += 1;
            return Task.CompletedTask;
        }

        #endregion

        #region Quiz

        public uint QuizPoints
        {
            get => mDbObject.QuizPoints;
            set => mDbObject.QuizPoints = value;
        }

        #endregion

        #region Offline TG

        public ushort MaxTrainingMinutes =>
            (ushort) Math.Min(1440 + 60 * VipLevel, (mDbObject.HeavenBlessing.Value - DateTime.Now).TotalMinutes);

        public ushort CurrentTrainingMinutes => //600;
            (ushort) Math.Min((DateTime.Now - mDbObject.LoginTime).TotalMinutes * 10, MaxTrainingMinutes);

        public ushort CurrentOfflineTrainingTime
        {
            get
            {
                if (mDbObject.AutoExercise == 0 || mDbObject.LogoutTime2 == null)
                    return 0;

                DateTime endTime = mDbObject.LogoutTime2.Value.AddMinutes(mDbObject.AutoExercise);
                if (endTime < DateTime.Now)
                    return CurrentTrainingTime;

                var remainingTime = (int) Math.Min((DateTime.Now - mDbObject.LogoutTime2.Value).TotalMinutes,
                                                   CurrentTrainingTime);
                return (ushort) remainingTime;
            }
        }

        public ushort CurrentTrainingTime => mDbObject.AutoExercise;

        public bool IsOfflineTraining => mDbObject.AutoExercise != 0;

        public async Task EnterAutoExerciseAsync()
        {
            if (!IsBlessed)
                return;

            mDbObject.AutoExercise = CurrentTrainingMinutes;
            mDbObject.LogoutTime2 = DateTime.Now;
        }

        public async Task LeaveAutoExerciseAsync()
        {
            await AwardExperienceAsync(CalculateExpBall(GetAutoExerciseExpTimes()), true);

            int totalMinutes = Math.Min(CurrentTrainingTime, CurrentOfflineTrainingTime);

            const int moneyPerMinute = 100;
            const double conquerPointsChance = 0.0125;

            await AwardMoneyAsync(moneyPerMinute * totalMinutes);

            var emoneyAmount = 0;
            for (var i = 0; i < totalMinutes; i++)
                if (await Kernel.ChanceCalcAsync(conquerPointsChance))
                    emoneyAmount += await Kernel.NextAsync(1, 3);

            if (emoneyAmount > 0)
                await AwardConquerPointsAsync(emoneyAmount);

            await FlyMapAsync(RecordMapIdentity, RecordMapX, RecordMapY);

            mDbObject.AutoExercise = 0;
            mDbObject.LogoutTime2 = null;
            await SaveAsync();
        }

        public int GetAutoExerciseExpTimes()
        {
            const int MAX_REWARD = 3000; // 5 Exp Balls every 8 hours
            const double REWARD_EVERY_N_MINUTES = 480;
            return (int) (Math.Min(CurrentOfflineTrainingTime, CurrentTrainingTime) / REWARD_EVERY_N_MINUTES *
                          MAX_REWARD);
        }

        public (int Level, ulong Experience) GetCurrentOnlineTGExp()
        {
            return PreviewExpBallUsage(GetAutoExerciseExpTimes());
        }

        #endregion

        #region Bonus

        public async Task<bool> DoBonusAsync()
        {
            if (!UserPackage.IsPackSpare(10))
            {
                await SendAsync(string.Format(Language.StrNotEnoughSpaceN, 10));
                return false;
            }

            DbBonus bonus = await BonusRepository.GetAsync(mDbObject.AccountIdentity);
            if (bonus == null || bonus.Flag != 0 || bonus.Time != null)
            {
                await SendAsync(Language.StrNoBonus);
                return false;
            }

            bonus.Flag = 1;
            bonus.Time = DateTime.Now;
            await ServerDbContext.SaveAsync(bonus);
            if (!await GameAction.ExecuteActionAsync(bonus.Action, this, null, null, ""))
            {
                await Log.GmLogAsync("bonus_error",
                                     $"{bonus.Identity},{bonus.AccountIdentity},{Identity},{bonus.Action}");
                return false;
            }

            await Log.GmLogAsync("bonus", $"{bonus.Identity},{bonus.AccountIdentity},{Identity},{bonus.Action}");
            return true;
        }

        public async Task<int> BonusCountAsync()
        {
            return await BonusRepository.CountAsync(mDbObject.AccountIdentity);
        }

        public async Task<bool> DoCardsAsync()
        {
            List<DbCard> cards = await CardRepository.GetAsync(mDbObject.AccountIdentity);
            if (cards.Count == 0)
                return false;

            int inventorySpace = cards.Count(x => x.ItemType != 0);
            if (inventorySpace > 0 && !UserPackage.IsPackSpare(inventorySpace))
            {
                await SendAsync(string.Format(Language.StrNotEnoughSpaceN, inventorySpace));
                return false;
            }

            var money = 0;
            var emoney = 0;
            var emoneyMono = 0;
            foreach (DbCard card in cards)
            {
                if (card.ItemType != 0)
                    await UserPackage.AwardItemAsync(card.ItemType);

                if (card.Money != 0)
                    money += (int) card.Money;

                if (card.ConquerPoints != 0)
                    emoney += (int) card.ConquerPoints;

                if (card.ConquerPointsMono != 0)
                    emoneyMono += (int) card.ConquerPointsMono;

                card.Flag |= 0x1;
                card.Timestamp = DateTime.Now;
            }

            await ServerDbContext.SaveAsync(cards);

            if (money > 0)
                await AwardMoneyAsync(money);

            if (emoney > 0)
                await AwardConquerPointsAsync(emoney);
            return true;
        }

        public Task<int> CardsCountAsync()
        {
            return CardRepository.CountAsync(mDbObject.AccountIdentity);
        }

        #endregion

        #region User Secondary Password

        public ulong SecondaryPassword
        {
            get => mDbObject.LockKey;
            set => mDbObject.LockKey = value;
        }

        public bool IsUnlocked()
        {
            return SecondaryPassword == 0 || VarData[0] != 0;
        }

        public void UnlockSecondaryPassword()
        {
            VarData[0] = 1;
        }

        public bool CanUnlock2ndPassword()
        {
            return VarData[1] <= 2;
        }

        public void Increment2ndPasswordAttempts()
        {
            VarData[1] += 1;
        }

        public async Task SendSecondaryPasswordInterfaceAsync()
        {
            await GameAction.ExecuteActionAsync(100, this, null, null, string.Empty);
        }

        #endregion

        #region Home

        public uint HomeIdentity
        {
            get => mDbObject?.HomeIdentity ?? 0u;
            set => mDbObject.HomeIdentity = value;
        }

        #endregion

        #region Revive

        public bool CanRevive()
        {
            return !IsAlive && mRevive.IsTimeOut();
        }

        public async Task RebornAsync(bool chgMap, bool isSpell = false)
        {
            if (IsAlive || !CanRevive() && !isSpell)
            {
                if (QueryStatus(StatusSet.GHOST) != null) await DetachStatusAsync(StatusSet.GHOST);

                if (QueryStatus(StatusSet.DEAD) != null) await DetachStatusAsync(StatusSet.DEAD);

                if (TransformationMesh == 98 || TransformationMesh == 99)
                    await ClearTransformationAsync();
                return;
            }

            BattleSystem.ResetBattle();

            await DetachStatusAsync(StatusSet.GHOST);
            await DetachStatusAsync(StatusSet.DEAD);

            await ClearTransformationAsync();

            await SetAttributesAsync(ClientUpdateType.Stamina, DEFAULT_USER_ENERGY);
            await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
            await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
            await SetXpAsync(0);

            if (CurrentEvent != null)
            {
                await CurrentEvent.OnReviveAsync(this, !isSpell);

                if (isSpell)
                {
                    await FlyMapAsync(mIdMap, mPosX, mPosY);
                }
                else
                {
                    (uint id, ushort x, ushort y) revive = await CurrentEvent.GetRevivePositionAsync(this);
                    await FlyMapAsync(revive.id, revive.x, revive.y);
                }
            }
            else if (chgMap || !IsBlessed && !isSpell)
            {
                await FlyMapAsync(mDbObject.MapID, mDbObject.X, mDbObject.Y);
            }
            else
            {
                if (!isSpell && (Map.IsPrisionMap()
                                 || Map.IsPkField()
                                 || Map.IsPkGameMap()
                                 || Map.IsSynMap()))
                    await FlyMapAsync(mDbObject.MapID, mDbObject.X, mDbObject.Y);
                else
                    await FlyMapAsync(mIdMap, mPosX, mPosY);
            }

            mRespawn.Startup(CHGMAP_LOCK_SECS);
        }

        #endregion

        #region Battle

        public override bool IsBowman => UserPackage[Item.ItemPosition.RightHand]?.IsBow() == true;

        public override bool IsShieldUser => UserPackage[Item.ItemPosition.LeftHand]?.IsShield() == true;

        public override bool SetAttackTarget(Role target)
        {
            if (target == null)
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (!target.IsAttackable(this))
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (target.IsWing && !IsWing && !IsBowman)
            {
                BattleSystem.ResetBattle();
                return false;
            }

            if (IsBowman && !IsArrowPass(target.MapX, target.MapY, 60))
                return false;

            if (QueryStatus(StatusSet.FATAL_STRIKE) != null)
            {
                if (GetDistance(target) > Screen.VIEW_SIZE)
                    return false;
            }
            else
            {
                if (GetDistance(target) > GetAttackRange(target.SizeAddition))
                {
                    BattleSystem.ResetBattle();
                    return false;
                }
            }

            if (CurrentEvent != null && !CurrentEvent.IsAttackEnable(this))
                return false;

            return true;
        }

        public Task AddSynWarScoreAsync(DynamicNpc npc, int score)
        {
            if (npc == null || score == 0)
                return Task.CompletedTask;

            if (Syndicate == null || npc.OwnerIdentity == SyndicateIdentity)
                return Task.CompletedTask;

            npc.AddSynWarScore(Syndicate, score);
            return Task.CompletedTask;
        }

        public async Task<bool> AutoSkillAttackAsync(Role target)
        {
            foreach (Magic magic in MagicData.Magics.Values)
            {
                float percent = magic.Percent;
                if (magic.AutoActive > 0
                    && Transformation == null
                    && (magic.WeaponSubtype == 0
                        || CheckWeaponSubType(magic.WeaponSubtype, magic.UseItemNum))
                    && await Kernel.ChanceCalcAsync(percent))
                    return await ProcessMagicAttackAsync(magic.Type, target.Identity, target.MapX, target.MapY,
                                                         magic.AutoActive);
            }

            return false;
        }

        public async Task SendGemEffectAsync()
        {
            var setGem = new List<Item.SocketGem>();

            for (var pos = Item.ItemPosition.EquipmentBegin; pos < Item.ItemPosition.EquipmentEnd; pos++)
            {
                Item item = UserPackage[pos];
                if (item == null)
                    continue;

                setGem.Add(item.SocketOne);
                if (item.SocketTwo != Item.SocketGem.NoSocket)
                    setGem.Add(item.SocketTwo);
            }

            int nGems = setGem.Count;
            if (nGems <= 0)
                return;

            var strEffect = "";
            switch (setGem[await Kernel.NextAsync(0, nGems)])
            {
                case Item.SocketGem.SuperPhoenixGem:
                    strEffect = "phoenix";
                    break;
                case Item.SocketGem.SuperDragonGem:
                    strEffect = "goldendragon";
                    break;
                case Item.SocketGem.SuperFuryGem:
                    strEffect = "fastflash";
                    break;
                case Item.SocketGem.SuperRainbowGem:
                    strEffect = "rainbow";
                    break;
                case Item.SocketGem.SuperKylinGem:
                    strEffect = "goldenkylin";
                    break;
                case Item.SocketGem.SuperVioletGem:
                    strEffect = "purpleray";
                    break;
                case Item.SocketGem.SuperMoonGem:
                    strEffect = "moon";
                    break;
            }

            await SendEffectAsync(strEffect, true);
        }

        public async Task SendWeaponMagic2Async(Role pTarget = null)
        {
            Item item = null;

            if (UserPackage[Item.ItemPosition.RightHand] != null &&
                UserPackage[Item.ItemPosition.RightHand].Effect != Item.ItemEffect.None)
                item = UserPackage[Item.ItemPosition.RightHand];
            if (UserPackage[Item.ItemPosition.LeftHand] != null &&
                UserPackage[Item.ItemPosition.LeftHand].Effect != Item.ItemEffect.None)
                if (item != null && await Kernel.ChanceCalcAsync(50f) || item == null)
                    item = UserPackage[Item.ItemPosition.LeftHand];

            if (item != null)
                switch (item.Effect)
                {
                    case Item.ItemEffect.Life:
                    {
                        if (!await Kernel.ChanceCalcAsync(15f))
                            return;
                        await AddAttributesAsync(ClientUpdateType.Hitpoints, 310);
                        var msg = new MsgMagicEffect
                        {
                            AttackerIdentity = Identity,
                            MagicIdentity = 1005
                        };
                        msg.Append(Identity, 310, false);
                        await BroadcastRoomMsgAsync(msg, true);
                        break;
                    }

                    case Item.ItemEffect.Mana:
                    {
                        if (!await Kernel.ChanceCalcAsync(17.5f))
                            return;
                        await AddAttributesAsync(ClientUpdateType.Mana, 310);
                        var msg = new MsgMagicEffect
                        {
                            AttackerIdentity = Identity,
                            MagicIdentity = 1195
                        };
                        msg.Append(Identity, 310, false);
                        await BroadcastRoomMsgAsync(msg, true);
                        break;
                    }

                    case Item.ItemEffect.Poison:
                    {
                        if (pTarget == null)
                            return;

                        if (!await Kernel.ChanceCalcAsync(5f))
                            return;

                        var msg = new MsgMagicEffect
                        {
                            AttackerIdentity = Identity,
                            MagicIdentity = 1320
                        };
                        msg.Append(pTarget.Identity, 210, true);
                        await BroadcastRoomMsgAsync(msg, true);

                        await pTarget.AttachStatusAsync(this, StatusSet.POISONED, 310, POISONDAMAGE_INTERVAL, 20, 0);

                        (int Damage, InteractionEffect Effect) result = await AttackAsync(pTarget);
                        int nTargetLifeLost = result.Damage;

                        await SendDamageMsgAsync(pTarget.Identity, nTargetLifeLost);

                        if (!pTarget.IsAlive)
                        {
                            var dwDieWay = 1;
                            if (nTargetLifeLost > pTarget.MaxLife / 3)
                                dwDieWay = 2;

                            await KillAsync(pTarget, IsBowman ? 5 : (uint) dwDieWay);
                        }

                        break;
                    }
                }
        }

        public async Task SendGemEffect2Async()
        {
            var setGem = new List<int>();

            for (var pos = Item.ItemPosition.EquipmentBegin; pos < Item.ItemPosition.EquipmentEnd; pos++)
            {
                Item item = UserPackage[pos];
                if (item == null)
                    continue;

                if (item.Blessing > 0)
                    setGem.Add(item.Blessing);
            }

            int nGems = setGem.Count;
            if (nGems <= 0)
                return;

            var strEffect = "";
            switch (setGem[await Kernel.NextAsync(0, nGems)])
            {
                case 1:
                    strEffect = "Aegis1";
                    break;
                case 3:
                    strEffect = "Aegis2";
                    break;
                case 5:
                    strEffect = "Aegis3";
                    break;
                case 7:
                    strEffect = "Aegis4";
                    break;
            }

            await SendEffectAsync(strEffect, true);
        }

        public async Task<long> CalcExpLostOfDeathAsync(Role killer)
        {
            if (killer is not Character)
                return 0;

            var param = 50;
            if (QueryStatus(StatusSet.RED_NAME) != null)
                param = 20;
            else if (QueryStatus(StatusSet.BLACK_NAME) != null)
                param = 10;

            var expLost = (int) ((long) Experience / param);

            if (SyndicateIdentity != 0)
            {
                const int moneyCostPerExp = 100;

                var decPercent = 0;
                var expPayBySyn = 0;
                if (Syndicate.Money > 0)
                {
                    int fundLost = Calculations.MulDiv(expLost, decPercent, 100 * moneyCostPerExp);
                    if (fundLost > Syndicate.Money)
                        fundLost = Syndicate.Money;

                    Syndicate.Money -= fundLost;

                    expPayBySyn = fundLost * moneyCostPerExp;
                    expLost -= expPayBySyn;
                }

                if (expPayBySyn > 0)
                    await SendAsync(string.Format(Language.StrExpLostBySynFund, expPayBySyn));
            }

            if (expLost > 0 && VipLevel >= 3)
                expLost /= 2;

            return Math.Max(0, expLost);
        }

        #endregion

        #region Rebirth

        public async Task<bool> RebirthAsync(ushort prof, ushort look)
        {
            DbRebirth data = ExperienceManager.GetRebirth(Profession, prof, Metempsychosis + 1);

            if (data == null)
            {
                if (IsPm())
                    await SendAsync($"No rebirth set for {Profession} -> {prof}");
                return false;
            }

            if (Level < data.NeedLevel)
            {
                await SendAsync(Language.StrNotEnoughLevel);
                return false;
            }

            if (Level >= 130)
            {
                DbLevelExperience levExp = ExperienceManager.GetLevelExperience(Level);
                if (levExp != null)
                {
                    float fExp = Experience / (float) levExp.Exp;
                    var metLev = (uint) (Level * 10000 + fExp * 1000);
                    if (metLev > mDbObject.MeteLevel)
                        mDbObject.MeteLevel = metLev;
                }
                else if (Level >= MAX_UPLEV)
                {
                    mDbObject.MeteLevel = MAX_UPLEV * 10000;
                }
            }

            int metempsychosis = Math.Min(Math.Max((byte) 1, Metempsychosis), (byte) 2);
            int oldProf = Profession;
            await ResetUserAttributesAsync(Metempsychosis, prof, look, data.NewLevel);

            for (var pos = Item.ItemPosition.EquipmentBegin; pos <= Item.ItemPosition.EquipmentEnd; pos++)
                if (UserPackage[pos] != null)
                    await UserPackage[pos].DegradeItemAsync(false);

            List<ushort> removeSkills = ExperienceManager
                                        .GetMagictypeOp(MagicTypeOp.MagictypeOperation.RemoveOnRebirth, oldProf / 10,
                                                        prof / 10, metempsychosis)?.Magics;
            List<ushort> resetSkills = ExperienceManager
                                       .GetMagictypeOp(MagicTypeOp.MagictypeOperation.ResetOnRebirth, oldProf / 10,
                                                       prof / 10, metempsychosis)?.Magics;
            List<ushort> learnSkills = ExperienceManager
                                       .GetMagictypeOp(MagicTypeOp.MagictypeOperation.LearnAfterRebirth, oldProf / 10,
                                                       prof / 10, metempsychosis)?.Magics;

            if (removeSkills != null)
                foreach (ushort skill in removeSkills)
                    await MagicData.UnlearnMagicAsync(skill, true);

            if (resetSkills != null)
                foreach (ushort skill in resetSkills)
                    await MagicData.ResetMagicAsync(skill);

            if (learnSkills != null)
                foreach (ushort skill in learnSkills)
                    await MagicData.CreateAsync(skill, 0);

            if (UserPackage[Item.ItemPosition.LeftHand]?.IsArrowSort() == false)
                await UserPackage.UnEquipAsync(Item.ItemPosition.LeftHand);

            if (UserPackage[Item.ItemPosition.RightHand]?.IsBow() == true && ProfessionSort != 4)
                await UserPackage.UnEquipAsync(Item.ItemPosition.RightHand);

            return true;
        }

        public async Task ResetUserAttributesAsync(byte mete, ushort newProf, ushort newLook, int newLev)
        {
            if (newProf == 0) newProf = (ushort) (Profession / 10 * 10 + 1);
            var prof = (byte) (newProf > 100 ? 10 : newProf / 10);

            int force = 0, speed = 0, health = 0, soul = 0;
            DbPointAllot pointAllot = ExperienceManager.GetPointAllot(prof, 1);
            if (pointAllot != null)
            {
                force = pointAllot.Strength;
                speed = pointAllot.Agility;
                health = pointAllot.Vitality;
                soul = pointAllot.Spirit;
            }
            else if (prof == 1)
            {
                force = 5;
                speed = 2;
                health = 3;
                soul = 0;
            }
            else if (prof == 2)
            {
                force = 5;
                speed = 2;
                health = 3;
                soul = 0;
            }
            else if (prof == 4)
            {
                force = 2;
                speed = 7;
                health = 1;
                soul = 0;
            }
            else if (prof == 10)
            {
                force = 0;
                speed = 2;
                health = 3;
                soul = 5;
            }
            else
            {
                force = 5;
                speed = 2;
                health = 3;
                soul = 0;
            }

            AutoAllot = false;

            int newAttrib = GetRebirthAddPoint(Profession, Level, mete) + newLev * 3;
            await SetAttributesAsync(ClientUpdateType.Atributes, (ulong) newAttrib);
            await SetAttributesAsync(ClientUpdateType.Strength, (ulong) force);
            await SetAttributesAsync(ClientUpdateType.Agility, (ulong) speed);
            await SetAttributesAsync(ClientUpdateType.Vitality, (ulong) health);
            await SetAttributesAsync(ClientUpdateType.Spirit, (ulong) soul);
            await SetAttributesAsync(ClientUpdateType.Hitpoints, MaxLife);
            await SetAttributesAsync(ClientUpdateType.Mana, MaxMana);
            await SetAttributesAsync(ClientUpdateType.Stamina, MaxEnergy);
            await SetAttributesAsync(ClientUpdateType.XpCircle, 0);

            if (newLook > 0 && newLook != Mesh % 10)
                await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);

            await SetAttributesAsync(ClientUpdateType.Level, (ulong) newLev);
            await SetAttributesAsync(ClientUpdateType.Experience, 0);

            if (mete == 0)
            {
                FirstProfession = Profession;
                mete++;
            }
            else if (mete == 1)
            {
                PreviousProfession = Profession;
                mete++;
            }
            else
            {
                FirstProfession = PreviousProfession;
                PreviousProfession = Profession;
            }


            await SetAttributesAsync(ClientUpdateType.Class, newProf);
            await SetAttributesAsync(ClientUpdateType.Reborn, mete);
            await SaveAsync();
        }

        public int GetRebirthAddPoint(int oldProf, int oldLev, int metempsychosis)
        {
            var points = 0;

            if (metempsychosis == 0)
            {
                if (oldProf == HIGHEST_WATER_WIZARD_PROF)
                    points += Math.Min((1 + (oldLev - 110) / 2) * ((oldLev - 110) / 2) / 2, 55);
                else
                    points += Math.Min((1 + (oldLev - 120)) * (oldLev - 120) / 2, 55);
            }
            else
            {
                if (oldProf == HIGHEST_WATER_WIZARD_PROF)
                    points += 52 + Math.Min((1 + (oldLev - 110) / 2) * ((oldLev - 110) / 2) / 2, 55);
                else
                    points += 52 + Math.Min((1 + (oldLev - 120)) * (oldLev - 120) / 2, 55);
            }

            return points;
        }

        public async Task<bool> UnlearnAllSkillAsync()
        {
            return await WeaponSkill.UnearnAllAsync();
        }

        #endregion

        #region Status

        public bool IsAway { get; set; }

        public async Task LoadStatusAsync()
        {
            List<DbStatus> statusList = await StatusRepository.GetAsync(Identity);
            foreach (DbStatus status in statusList)
            {
                if (status.EndTime < DateTime.Now)
                {
                    await ServerDbContext.DeleteAsync(status);
                    continue;
                }

                await AttachStatusAsync(status);
            }

            if (VipLevel > 0)
                await AttachStatusAsync(this, StatusSet.ORANGE_HALO_GLOW, 0, (int)(VipExpiration - DateTime.Now).TotalSeconds, 0, 0, true);
        }

        #endregion

        #region Vigor

        public int Vigor { get; set; }

        public int MaxVigor =>
            QueryStatus(StatusSet.RIDING) != null ? UserPackage[Item.ItemPosition.Steed]?.Vigor ?? 0 : 0;

        public void UpdateVigorTimer()
        {
            mVigorTimer.Update();
        }

        #endregion

        #region Mining

        private int mMineCount;

        public void StartMining()
        {
            mMine.Startup(3);
            mMineCount = 0;
        }

        public void StopMining()
        {
            mMine.Clear();
        }

        public async Task DoMineAsync()
        {
            if (!IsAlive)
            {
                await SendAsync(Language.StrDead);
                StopMining();
                return;
            }

            if (!Map.IsMineField())
            {
                await SendAsync(Language.StrNoMine);
                StopMining();
                return;
            }

            if (UserPackage[Item.ItemPosition.RightHand]?.GetItemSubType() != 562)
            {
                await SendAsync(Language.StrMineWithPecker);
                StopMining();
                return;
            }

            try
            {
                if (UserPackage.IsPackFull())
                {
                    await SendAsync(Language.StrYourBagIsFull);
                }
                else
                {
                    uint idItem = 0;
                    float nChance = 30f + (float) (WeaponSkill[562]?.Level ?? 0) / 2;
                    if (await Kernel.ChanceCalcAsync(nChance))
                    {
                        const int euxiniteOre = 1072031;
                        const int ironOre = 1072010;
                        const int copperOre = 1072020;
                        const int silverOre = 1072040;
                        const int goldOre = 1072050;
                        int oreRate = await Kernel.NextAsync(100);
                        int oreLevel = await Kernel.NextAsync(10) % 10;
                        switch (Map.ResLev) // TODO gems
                        {
                            case 1:
                            {
                                if (oreRate < 4) // 4% Euxinite
                                    idItem = euxiniteOre;
                                else if (oreRate < 6) // 6% Gold Ore
                                    idItem = (uint) (goldOre + oreLevel);
                                else if (oreRate < 50) // 40% Iron Ore
                                    idItem = (uint) (ironOre + oreLevel);
                                break;
                            }
                            case 2:
                            {
                                if (oreRate < 5) // 5% Gold Ore
                                    idItem = (uint) (goldOre + oreLevel);
                                else if (oreRate < 15) // 10% Copper Ore
                                    idItem = (uint) (copperOre + oreLevel);
                                else if (oreRate < 50) // 35% Iron Ore
                                    idItem = (uint) (ironOre + oreLevel);
                                break;
                            }
                            case 3:
                            {
                                if (oreRate < 5) // 5% Gold Ore
                                    idItem = (uint) (goldOre + oreLevel);
                                else if (oreRate < 12) // 7% Silver Ore
                                    idItem = (uint) (silverOre + oreLevel);
                                else if (oreRate < 25) // 13% Copper Ore
                                    idItem = (uint) (copperOre + oreLevel);
                                else if (oreRate < 50) // 25% Iron Ore
                                    idItem = (uint) (ironOre + oreLevel);
                                break;
                            }
                        }
                    }
                    else
                    {
                        idItem = await MineManager.MineAsync(MapIdentity, this);
                    }

                    DbItemtype itemtype = ItemManager.GetItemtype(idItem);
                    if (itemtype == null)
                        return;

                    if (await UserPackage.AwardItemAsync(idItem))
                    {
                        await SendAsync(string.Format(Language.StrMineItemFound, itemtype.Name));
                        await Log.GmLogAsync("mine_drop",
                                             $"{Identity},{Name},{idItem},{MapIdentity},{Map?.Name},{MapX},{MapY}");
                    }

                    mMineCount++;
                }
            }

            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Mining error");
                await Log.WriteLogAsync(ex);
            }
            finally
            {
                await BroadcastRoomMsgAsync(new MsgAction
                {
                    Identity = Identity,
                    Command = 0,
                    ArgumentX = MapX,
                    ArgumentY = MapY,
                    Action = MsgAction<Client>.ActionType.MapMine
                }, true);
            }
        }

        #endregion

        #region Battle

        public async Task<int> GetInterAtkRateAsync()
        {
            int nRate = USER_ATTACK_SPEED;
            int nRateR = 0, nRateL = 0;

            if (UserPackage[Item.ItemPosition.RightHand] != null)
                nRateR = UserPackage[Item.ItemPosition.RightHand].Itemtype.AtkSpeed;
            if (UserPackage[Item.ItemPosition.LeftHand] != null &&
                !UserPackage[Item.ItemPosition.LeftHand].IsArrowSort())
                nRateL = UserPackage[Item.ItemPosition.LeftHand].Itemtype.AtkSpeed;

            if (nRateR > 0 && nRateL > 0)
                nRate = (nRateR + nRateL) / 2;
            else if (nRateR > 0)
                nRate = nRateR;
            else if (nRateL > 0)
                nRate = nRateL;

//#if DEBUG
            if (QueryStatus(StatusSet.CYCLONE) != null)
            {
                nRate = Calculations.CutTrail(0,
                                              Calculations.AdjustData(nRate, QueryStatus(StatusSet.CYCLONE).Power));
                if (IsPm())
                    await SendAsync($"attack speed+: {nRate}");
            }
//#endif

            return Math.Max(400, nRate);
        }

        public override int GetAttackRange(int sizeAdd)
        {
            int nRange = 1, nRangeL = 0, nRangeR = 0;

            if (UserPackage[Item.ItemPosition.RightHand] != null && UserPackage[Item.ItemPosition.RightHand].IsWeapon())
                nRangeR = UserPackage[Item.ItemPosition.RightHand].AttackRange;
            if (UserPackage[Item.ItemPosition.LeftHand] != null && UserPackage[Item.ItemPosition.LeftHand].IsWeapon())
                nRangeL = UserPackage[Item.ItemPosition.LeftHand].AttackRange;

            if (nRangeR > 0 && nRangeL > 0)
                nRange = (nRangeR + nRangeL) / 2;
            else if (nRangeR > 0)
                nRange = nRangeR;
            else if (nRangeL > 0)
                nRange = nRangeL;

            nRange += (SizeAddition + sizeAdd + 1) / 2;

            return nRange + 1;
        }

        public override bool IsImmunity(Role target)
        {
            if (base.IsImmunity(target))
                return true;

            if (target is Character user)
                switch (PkMode)
                {
                    case PkModeType.Capture:
                        if (MagicData.QueryMagic?.Crime == 0)
                            return false;
                        return !user.IsEvil();
                    case PkModeType.Peace:
                        return true;
                    case PkModeType.FreePk:
                        if (Level >= 26 && user.Level < 26)
                            return true;
                        return false;
                    case PkModeType.Team:
                        if (IsFriend(user.Identity))
                            return true;
                        if (IsMate(user.Identity))
                            return true;
                        if (Map?.IsFamilyMap() == true)
                        {
                            if (Family.GetMember(user.Identity) != null
                                || user.Family?.IsAlly(FamilyIdentity) == true)
                                return true;
                        }
                        else
                        {
                            if (Syndicate?.QueryMember(user.Identity) != null)
                                return true;
                            if (Syndicate?.IsAlly(user.SyndicateIdentity) == true)
                                return true;
                            if (Team?.IsMember(user.Identity) == true)
                                return true;
                        }

                        return false;
                }
            else if (target is Monster monster)
                switch (PkMode)
                {
                    case PkModeType.Peace:
                        return false;
                    case PkModeType.Team:
                    case PkModeType.Capture:
                        if (monster.IsGuard() || monster.IsPkKiller())
                            return true;
                        if (monster.IsCallPet())
                        {
                            Character owner = RoleManager.GetUser(monster.OwnerIdentity);
                            if (owner?.IsCrime() != true)
                                return true;
                        }
                        return false;
                    case PkModeType.FreePk:
                        return false;
                }
            else if (target is DynamicNpc dynaNpc) return false;

            return true;
        }

        public override bool IsAttackable(Role attacker)
        {
            if (attacker is Character && Map.IsPkDisable())
                return false;

            return (!mRespawn.IsActive() || mRespawn.IsTimeOut()) && IsAlive &&
                   !(attacker is Character && Map.QueryRegion(RegionTypes.PkProtected, MapX, MapY));
        }

        public override async Task<(int Damage, InteractionEffect Effect)> AttackAsync(Role target)
        {
            if (target == null)
                return (0, InteractionEffect.None);

            if (!target.IsEvil() && Map.IsDeadIsland() 
                || target is Monster mob && (mob.IsGuard() || mob.IsCallPet()))
                await SetCrimeStatusAsync(15);

            return await BattleSystem.CalcPowerAsync(BattleSystem.MagicType.None, this, target);
        }

        public override async Task KillAsync(Role target, uint dieWay)
        {
            if (target == null)
                return;

            if (target is Character targetUser)
            {
                await BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.Kill,
                    SenderIdentity = Identity,
                    TargetIdentity = target.Identity,
                    PosX = target.MapX,
                    PosY = target.MapY,
                    Data = (int) dieWay
                }, true);

                if (MagicData.QueryMagic != null && MagicData.QueryMagic.Sort != MagicData.MagicSort.Activateswitch)
                    await ProcessPkAsync(targetUser);

                if (targetUser.IsBlessed && !IsBlessed)
                {
                    if (QueryStatus(StatusSet.CURSED) == null)
                    {
                        await AttachStatusAsync(this, StatusSet.CURSED, 0, 300, 0, 0, true);
                    }
                    else
                    {
                        QueryStatus(StatusSet.CURSED).IncTime(300000, int.MaxValue);
                        await QueryStatus(StatusSet.CURSED)
                            .ChangeDataAsync(0, QueryStatus(StatusSet.CURSED).RemainingTime);
                    }
                }
            }
            else if (target is Monster monster)
            {
                await AddXpAsync(1);

                if (QueryStatus(StatusSet.CYCLONE) != null || QueryStatus(StatusSet.SUPERMAN) != null)
                {
                    KoCount += 1;
                    IStatus status = QueryStatus(StatusSet.CYCLONE) ?? QueryStatus(StatusSet.SUPERMAN);
                    status?.IncTime(700, 30000);
                }

                if (!(MessageBox is CaptchaBox))
                    mKillsToCaptcha++;

                if (!(MessageBox is CaptchaBox)
                    && mKillsToCaptcha > 5000 + await Kernel.NextAsync(1500)
                    && await Kernel.ChanceCalcAsync(50, 10000))
                {
                    var captcha = (CaptchaBox) (MessageBox = new CaptchaBox(this));
                    await captcha.GenerateAsync();
                    mKillsToCaptcha = 0;
                }

                await KillMonsterAsync(monster.Type);
            }

            await target.BeKillAsync(this);

            if (CurrentEvent != null)
                await CurrentEvent.OnKillAsync(this, target, MagicData.QueryMagic);

            await GameAction.ExecuteActionAsync(USER_KILL_ACTION, this, target, null, string.Empty);
        }

        public override async Task<bool> BeAttackAsync(BattleSystem.MagicType magic, Role attacker, int power,
                                                       bool bReflectEnable)
        {
            if (attacker == null)
                return false;

            if (IsLucky && await Kernel.ChanceCalcAsync(1, 100))
            {
                await SendEffectAsync("LuckyGuy", true);
                power /= 10;
            }

            if ((PreviousProfession == 25 || FirstProfession == 25) && bReflectEnable &&
                await Kernel.ChanceCalcAsync(5, 100))
            {
                power = Math.Min(1700, power);

                await attacker.BeAttackAsync(magic, this, power, false);
                await BroadcastRoomMsgAsync(new MsgInteract
                {
                    Action = MsgInteractType.ReflectMagic,
                    Data = power,
                    PosX = MapX,
                    PosY = MapY,
                    SenderIdentity = Identity,
                    TargetIdentity = attacker.Identity
                }, true);

                if (!attacker.IsAlive)
                    await attacker.BeKillAsync(null);

                return true;
            }

            if (CurrentEvent != null)
                await CurrentEvent.OnBeAttackAsync(attacker, this, (int) Math.Min(Life, power));

            if (power > 0)
            {
                await AddAttributesAsync(ClientUpdateType.Hitpoints, power * -1);
                _ = BroadcastTeamLifeAsync().ConfigureAwait(false);
            }

            if (IsAlive && await Kernel.ChanceCalcAsync(5))
                await SendGemEffect2Async();

            if (!Map.IsTrainingMap())
                await DecEquipmentDurabilityAsync(true, (int) magic, (ushort) (power > MaxLife / 4 ? 10 : 1));

            if (MagicData.QueryMagic != null && MagicData.State == MagicData.MagicState.Intone)
                await MagicData.AbortMagicAsync(true);

            if (Action == EntityAction.Sit)
                await SetAttributesAsync(ClientUpdateType.Stamina, (ulong) (Energy / 2));
            return true;
        }

        public override async Task BeKillAsync(Role attacker)
        {
            if (QueryStatus(StatusSet.GHOST) != null)
                return;

            BattleSystem.ResetBattle();
            if (MagicData.QueryMagic != null)
                await MagicData.AbortMagicAsync(false);

            TransformationMesh = 0;
            Transformation = null;
            mTransformation.Clear();

            if (QueryStatus(StatusSet.CYCLONE) != null || QueryStatus(StatusSet.SUPERMAN) != null)
                await FinishXpAsync();

            await SetAttributesAsync(ClientUpdateType.Mesh, Mesh);

            await DetachStatusAsync(StatusSet.CRIME);
            await DetachAllStatusAsync();

            if (Scapegoat)
                await SetScapegoatAsync(false);

            await AttachStatusAsync(this, StatusSet.DEAD, 0, int.MaxValue, 0, 0);
            await AttachStatusAsync(this, StatusSet.GHOST, 0, int.MaxValue, 0, 0);

            mGhost.Startup(4);

            await KillCallPetAsync();

            if (attacker?.IsCallPet() == true && attacker.OwnerIdentity != 0)
            {
                Character owner = RoleManager.GetUser(attacker.OwnerIdentity);
                if (owner != null)
                    await owner.ProcessPkAsync(this);
            }

            await GameAction.ExecuteActionAsync(USER_DIE_ACTION, this, attacker, null, string.Empty);

            if (CurrentEvent is ArenaQualifier qualifier)
            {
                ArenaQualifier.QualifierMatch match = qualifier.FindMatchByMap(MapIdentity);
                if (match != null)
                {
                    await match.FinishAsync(null, this);
                    return;
                }
            }

            uint idMap = 0;
            var posTarget = new Point();
            if (Map.GetRebornMap(ref idMap, ref posTarget))
                await SavePositionAsync(idMap, (ushort) posTarget.X, (ushort) posTarget.Y);

            if (Map.IsPkField() || Map.IsSynMap())
            {
                if (!Map.IsDeadIsland())
                    await UserPackage.RandDropItemAsync(1, 30);
                if (Map.IsSynMap() && !Map.IsWarTime())
                    await SavePositionAsync(1002, 430, 378);
                return;
            }

            if (Map.IsPrisionMap())
            {
                if (!Map.IsDeadIsland())
                {
                    int nChance = Math.Min(90, 20 + PkPoints / 2);
                    await UserPackage.RandDropItemAsync(3, nChance);
                }

                return;
            }

            if (attacker == null)
                return;

            if (!Map.IsDeadIsland())
            {
                var nChance = 0;
                if (PkPoints < 30)
                    nChance = 10 + await Kernel.NextAsync(40);
                else if (PkPoints < 100)
                    nChance = 50 + await Kernel.NextAsync(50);
                else
                    nChance = 100;

                int nItems = UserPackage.InventoryCount;
                int nDropItem = Level < 15 ? 0 : nItems * nChance / 100;

                await UserPackage.RandDropItemAsync(nDropItem);

                uint moneyDropped;
                if (QueryStatus(StatusSet.RED_NAME) != null)                                   // Red name drop
                    moneyDropped = (uint) (Silvers * (await Kernel.NextAsync(35) + 25) / 100); // 25-60%
                else if (QueryStatus(StatusSet.BLACK_NAME) != null)                            // Black name drop
                    moneyDropped = (uint) (Silvers * (await Kernel.NextAsync(40) + 40) / 100); // 40-80%
                else                                                                           // normal drop
                    moneyDropped = (uint) (Silvers * (await Kernel.NextAsync(30) + 10) / 100); // 10-40%

                if (moneyDropped > 0)
                    await DropSilverAsync(moneyDropped);

                if (attacker.Identity != Identity && attacker is Character atkrUser)
                {
                    if (QueryStatus(StatusSet.CRIME) == null)
                        await CreateEnemyAsync(atkrUser);

                    if (!IsBlessed)
                    {
                        long expLost = await CalcExpLostOfDeathAsync(attacker);
                        if (expLost > 0)
                            await AddAttributesAsync(ClientUpdateType.Experience, expLost * -1);
                    }

                    if (!atkrUser.IsBlessed && IsBlessed)
                    {
                        if (atkrUser.QueryStatus(StatusSet.CURSED) != null)
                        {
                            IStatus status = atkrUser.QueryStatus(StatusSet.CURSED);
                            status.IncTime(300000, 60 * 5 * 12 * 1000);
                            await atkrUser.SynchroAttributesAsync(ClientUpdateType.CursedTimer,
                                                                  (ulong) status.RemainingTime);
                        }
                        else
                        {
                            await atkrUser.AttachStatusAsync(this, StatusSet.CURSED, 0, 300, 0, 0);
                        }
                    }

                    int detainAmount = 0;

                    if (PkPoints >= 300)
                    {
                        detainAmount = 2;
                    }
                    else if (PkPoints >= 100 || (PkPoints >= 30 && await Kernel.ChanceCalcAsync(40, 100)))
                    {
                        detainAmount = 1;
                    }

                    for (int i = 0; i < detainAmount; i++)
                    {
                        if (!await ItemManager.DetainItemAsync(this, atkrUser))
                            break;
                    }

                    if (PkPoints >= 100)
                    {
                        await SavePositionAsync(6000, 31, 72);
                        await FlyMapAsync(6000, 31, 72);
                        await RoleManager.BroadcastMsgAsync(
                            string.Format(Language.StrGoToJail, attacker.Name, Name), TalkChannel.Talk,
                            Color.White);
                    }
                }
            }
            else if (attacker is Character atkUser && Map.IsDeadIsland())
            {
                await CreateEnemyAsync(atkUser);
            }
            else if (attacker is Monster monster)
            {
                var dropMoney = (uint) await Kernel.NextAsync((int) (Silvers / 3));
                if (dropMoney > 0)
                    await DropSilverAsync(dropMoney);

                var chance = 33;
                if (Level < 10)
                    chance = 5;
                await UserPackage.RandDropItemAsync(0, chance);

                if (monster.IsGuard() && PkPoints > 99)
                {
                    await SavePositionAsync(6000, 31, 72);
                    await FlyMapAsync(6000, 31, 72);
                    await RoleManager.BroadcastMsgAsync(
                        string.Format(Language.StrGoToJail, attacker.Name, Name), TalkChannel.Talk,
                        Color.White);
                }
            }
        }

        #endregion

        #region Jar

        public async Task AddJarKillsAsync(int stcType)
        {
            Item jar = UserPackage.GetItemByType(Item.TYPE_JAR);
            if (jar != null)
                if (jar.MaximumDurability == stcType)
                {
                    jar.Data += 1;
                    await jar.SaveAsync();

                    if (jar.Data % 50 == 0) await jar.SendJarAsync();
                }
        }

        #endregion

        #region Arena Qualifier

        public int QualifierRank => EventManager.GetEvent<ArenaQualifier>()?.GetPlayerRanking(Identity) ?? 0;

        public ArenaStatus QualifierStatus { get; set; } = ArenaStatus.NotSignedUp;

        public uint QualifierPoints
        {
            get => mDbObject.AthletePoint;
            set => mDbObject.AthletePoint = value;
        }

        public uint QualifierDayWins
        {
            get => mDbObject.AthleteDayWins;
            set => mDbObject.AthleteDayWins = value;
        }

        public uint QualifierDayLoses
        {
            get => mDbObject.AthleteDayLoses;
            set => mDbObject.AthleteDayLoses = value;
        }

        public uint QualifierDayGames => QualifierDayWins + QualifierDayLoses;

        public uint QualifierHistoryWins
        {
            get => mDbObject.AthleteHistoryWins;
            set => mDbObject.AthleteHistoryWins = value;
        }

        public uint QualifierHistoryLoses
        {
            get => mDbObject.AthleteHistoryLoses;
            set => mDbObject.AthleteHistoryLoses = value;
        }

        public uint HonorPoints
        {
            get => mDbObject.AthleteCurrentHonorPoints;
            set => mDbObject.AthleteCurrentHonorPoints = value;
        }

        public uint HistoryHonorPoints
        {
            get => mDbObject.AthleteHistoryHonorPoints;
            set => mDbObject.AthleteHistoryHonorPoints = value;
        }

        public DbArenic DailyArenic { get; set; }

        #endregion

        #region Enlightment

        public const int ENLIGHTENMENT_MAX_TIMES = 5;
        public const int ENLIGHTENMENT_UPLEV_MAX_EXP = 600;
        public const int ENLIGHTENMENT_EXP_PART_TIME = 60 * 20;

        private const int EnlightenmentUserStc = 1127;

        public uint EnlightenPoints
        {
            get => mDbObject.MentorOpportunity;
            set => mDbObject.MentorOpportunity = value;
        }

        public uint EnlightenedTimes
        {
            get => mDbObject.MentorAchieve;
            set => mDbObject.MentorAchieve = value;
        }

        public uint EnlightenExperience
        {
            get => mDbObject.MentorUplevTime;
            set => mDbObject.MentorUplevTime = value;
        }

        public uint EnlightmentLastUpdate
        {
            get => mDbObject.MentorDay;
            set => mDbObject.MentorDay = value;
        }

        public void SetEnlightenLastUpdate()
        {
            EnlightmentLastUpdate = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
        }

        public bool CanBeEnlightened(Character mentor)
        {
            if (mentor == null) return false;
            if (EnlightenedTimes >= ENLIGHTENMENT_MAX_TIMES)
                return false;
            if (EnlightenExperience >= ENLIGHTENMENT_UPLEV_MAX_EXP / 2 * ENLIGHTENMENT_MAX_TIMES)
                return false;
            if (mentor.Level - Level < 20)
                return false;

            DbStatistic stc = Statistic.GetStc(EnlightenmentUserStc, mentor.Identity);
            if (stc?.Timestamp != null)
            {
                int day = int.Parse(stc.Timestamp.Value.ToString("yyyyMMdd"));
                int now = int.Parse(DateTime.Now.ToString("yyyyMMdd"));
                return day != now;
            }
            return true;
        }

        public async Task<bool> EnlightenPlayerAsync(Character target)
        {
            var enlightTimes = (int) (EnlightenPoints / 100);
            if (enlightTimes <= 0)
                return false;

            if (target.Level > Level - 20)
                // todo send message
                return false;

            if (!target.CanBeEnlightened(this))
                // todo send message
                return false;

            EnlightenPoints = Math.Max(EnlightenPoints - 100, 0);

            if (target.EnlightenedTimes == 0 || !mEnlightenTimeExp.IsActive())
                mEnlightenTimeExp.Startup(ENLIGHTENMENT_EXP_PART_TIME); // 20 minutes

            target.EnlightenedTimes += 1;
            target.EnlightenExperience += ENLIGHTENMENT_UPLEV_MAX_EXP / 2;

            await target.Statistic.AddOrUpdateAsync(EnlightenmentUserStc, Identity, 1, true);

            // we will send instand 300 uplev exp and 300 will be awarded for 5 minutes later
            await target.AwardExperienceAsync(CalculateExpBall(ENLIGHTENMENT_UPLEV_MAX_EXP / 2), true);
            await target.SendAsync(new MsgUserAttrib(Identity, ClientUpdateType.EnlightenPoints, 0));
            //await SynchroAttributesAsync(ClientUpdateType.EnlightenPoints, EnlightenPoints, true);

            await SaveAsync();
            await target.SaveAsync();
            return true;
        }

        public async Task ResetEnlightenmentAsync()
        {
            if (EnlightmentLastUpdate >= uint.Parse(DateTime.Now.ToString("yyyyMMdd")))
                return;

            EnlightmentLastUpdate = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));

            EnlightenedTimes = 0;

            EnlightenPoints = 0;
            if (Level >= 90)
                EnlightenPoints += 100;
            switch (NobilityRank)
            {
                case NobilityRank.Knight:
                case NobilityRank.Baron:
                    EnlightenPoints += 100;
                    break;
                case NobilityRank.Earl:
                case NobilityRank.Duke:
                    EnlightenPoints += 200;
                    break;
                case NobilityRank.Prince:
                    EnlightenPoints += 300;
                    break;
                case NobilityRank.King:
                    EnlightenPoints += 400;
                    break;
            }

            switch (VipLevel)
            {
                case 1:
                case 2:
                case 3:
                    EnlightenPoints += 100;
                    break;
                case 4:
                case 5:
                    EnlightenPoints += 200;
                    break;
                case 6:
                    EnlightenPoints += 300;
                    break;
            }
        }

        #endregion

        #region Call Pet

        private TimeOut mCallPetKeepSecs = new();
        private Monster mCallPet;

        public async Task<bool> CallPetAsync(uint type, ushort x, ushort y, int keepSecs = 0)
        {
            await KillCallPetAsync();

            Monster pet = await Monster.CreateCallPetAsync(this, type, x, y);
            if (pet == null)
                return false;

            mCallPet = pet;

            if (keepSecs > 0)
            {
                mCallPetKeepSecs.Startup(keepSecs);
            }
            else
            {
                mCallPetKeepSecs.Clear();
            }
            return true;
        }

        public async Task KillCallPetAsync(bool now = false)
        {
            if (mCallPet == null)
                return;

            if (!mCallPet.IsDeleted())
            {
                await mCallPet.DelMonsterAsync(now);
                mCallPet = null;
            }
        }

        public Role GetCallPet()
        {
            return mCallPet;
        }

        #endregion

        #region Player Pose

        private uint mCoupleInteractionTarget;
        private bool mCoupleInteractionStarted;

        public bool HasCoupleInteraction()
        {
            return mCoupleInteractionTarget != 0;
        }

        public Character GetCoupleInteractionTarget()
        {
            return RoleManager.GetUser(mCoupleInteractionTarget);
        }

        public EntityAction CoupleAction { get; private set; }

        public async Task<bool> SetActionAsync(EntityAction action, uint target)
        {
            // hum
            CoupleAction = action;
            mCoupleInteractionTarget = target;
            return true;
        }

        public void CancelCoupleInteraction()
        {
            CoupleAction = EntityAction.None;
            mCoupleInteractionTarget = 0;
            PopRequest(RequestType.CoupleInteraction);
            mCoupleInteractionStarted = false;
        }

        public void StartCoupleInteraction()
        {
            mCoupleInteractionStarted = true;
        }

        public bool HasCoupleInteractionStarted() => mCoupleInteractionStarted;

        #endregion

        #region On Timer

        public override async Task OnTimerAsync()
        {
            if (Map == null)
                return;

            try
            {
                if (MessageBox != null)
                    await MessageBox.OnTimerAsync();

                if (MessageBox?.HasExpired == true)
                    MessageBox = null;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Character::OnTimerAsync() => {Identity}:{Name} MessageBox.OnTimerAsync(): {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mPkDecrease.ToNextTime(PK_DEC_TIME) && PkPoints > 0)
                {
                    if (MapIdentity == 6001)
                        QueueAction(() => AddAttributesAsync(ClientUpdateType.PkPoints, PKVALUE_DEC_ONCE_IN_PRISON));
                    else
                        QueueAction(() => AddAttributesAsync(ClientUpdateType.PkPoints, PKVALUE_DEC_ONCE));
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} Pk Decrease: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                QueueAction(async () =>
                {
                    if (mEnlightenTimeExp.IsActive() && mEnlightenTimeExp.ToNextTime())
                    {
                        var amount = (int) Math.Min(ENLIGHTENMENT_UPLEV_MAX_EXP / 2, EnlightenExperience);
                        if (amount != 0)
                        {
                            await AwardExperienceAsync(amount, true);
                            EnlightenExperience -= (uint) amount;
                        }

                        if (EnlightenExperience == 0)
                            mEnlightenTimeExp.Clear();
                    }
                });
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Character::OnTimerAsync() => {Identity}:{Name} Enlighten Experience: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            // status has no exception block because if it breaks the queue will handle it
            if (mStatusTm.ToNextTime())
                QueueAction(async () =>
                {
                    foreach (IStatus status in StatusSet.Status.Values)
                    {
                        await status.OnTimerAsync();

                        if (!status.IsValid && status.Identity != StatusSet.GHOST && status.Identity != StatusSet.DEAD)
                        {
                            await StatusSet.DelObjAsync(status.Identity);

                            if ((status.Identity == StatusSet.SUPERMAN || status.Identity == StatusSet.CYCLONE) &&
                                QueryStatus(StatusSet.SUPERMAN) == null &&
                                QueryRole(StatusSet.CYCLONE) == null) await FinishXpAsync();
                        }
                    }
                });

            try
            {
                if (IsBlessed && mHeavenBlessing.ToNextTime() && !Map.IsTrainingMap())
                {
                    mBlessPoints++;
                    if (mBlessPoints >= 10)
                    {
                        GodTimeExp += 60;

                        await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 5);
                        await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 0);
                        mBlessPoints = 0;
                    }
                    else
                    {
                        await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 4);
                        await SynchroAttributesAsync(ClientUpdateType.OnlineTraining, 3);
                    }
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Character::OnTimerAsync() => {Identity}:{Name} Heaven Blessing: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mIdLuckyTarget == 0 && Metempsychosis < 2 && QueryStatus(StatusSet.LUCKY_DIFFUSE) == null)
                {
                    if (QueryStatus(StatusSet.LUCKY_ABSORB) == null)
                        foreach (Character user in Screen.Roles.Values.Where(x => x.IsPlayer()).Cast<Character>())
                            if (user.QueryStatus(StatusSet.LUCKY_DIFFUSE) != null && GetDistance(user) <= 3)
                            {
                                mIdLuckyTarget = user.Identity;
                                mLuckyAbsorbStart.Startup(3);
                                break;
                            }
                }
                else if (QueryStatus(StatusSet.LUCKY_DIFFUSE) == null)
                {
                    var role = QueryRole(mIdLuckyTarget) as Character;
                    if (mLuckyAbsorbStart.IsTimeOut() && role != null)
                    {
                        await AttachStatusAsync(role, StatusSet.LUCKY_ABSORB, 0, 1000000, 0, 0);
                        mIdLuckyTarget = 0;
                        mLuckyAbsorbStart.Clear();
                    }
                }

                if (mLuckyStep.ToNextTime() && IsLucky)
                    if (QueryStatus(StatusSet.LUCKY_DIFFUSE) == null && QueryStatus(StatusSet.LUCKY_ABSORB) == null)
                        await ChangeLuckyTimerAsync(-1);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} Lucky Time: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (!IsAlive && !IsGhost() && mGhost.IsActive() && mGhost.IsTimeOut(4))
                {
                    await SetGhostAsync();
                    mGhost.Clear();
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} Set Ghost: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (Team != null && !Team.IsLeader(Identity) && Team.Leader.MapIdentity == MapIdentity &&
                    mTeamLeaderPos.ToNextTime())
                    await SendAsync(new MsgAction
                    {
                        Action = MsgAction<Client>.ActionType.MapTeamLeaderStar,
                        Command = Team.Leader.Identity,
                        ArgumentX = Team.Leader.MapX,
                        ArgumentY = Team.Leader.MapY
                    });
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Character::OnTimerAsync() => {Identity}:{Name} Team synchro leader position: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (Guide != null && Guide.BetrayalCheck) QueueAction(() => Guide.BetrayalTimerAsync());

                foreach (Tutor apprentice in m_apprentices.Values.Where(x => x.BetrayalCheck))
                    QueueAction(() => apprentice.BetrayalTimerAsync());
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Character::OnTimerAsync() => {Identity}:{Name} Guide.BetrayalTimerAsync(): {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mVigorTimer.ToNextTime(1000) && QueryStatus(StatusSet.RIDING) != null && Vigor < MaxVigor)
                    await AddAttributesAsync(ClientUpdateType.Vigor,
                                             (long) Math.Max(10, Math.Min(200, MaxVigor * 0.01)));
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} Vigor Update: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mCallPet != null && mCallPetKeepSecs.IsActive() && mCallPetKeepSecs.ToNextTime())
                    await KillCallPetAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} Kill call pet: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            if (!IsAlive)
                return;

            try
            {
                if (BattleSystem != null
                    && BattleSystem.IsActive()
                    && BattleSystem.NextAttack(await GetInterAtkRateAsync()))
                    QueueAction(BattleSystem.ProcessAttackAsync);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Character::OnTimerAsync() => {Identity}:{Name} BattleSystem.ProcessAttackAsync(): {ex.Message}");
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
                    $"Character::OnTimerAsync() => {Identity}:{Name} MagicData.OnTimerAsync(): {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mVigorTimer.ToNextTime() && QueryStatus(StatusSet.RIDING) != null && Vigor < MaxVigor)
                    await AddAttributesAsync(ClientUpdateType.Vigor,
                                             (long) Math.Max(10, Math.Min(200, MaxVigor * 0.005)));
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} Vigor Check: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (Transformation != null && mTransformation.IsTimeOut())
                    await ClearTransformationAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Character::OnTimerAsync() => {Identity}:{Name} Clear Transformation: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mEnergyTm.ToNextTime(ADD_ENERGY_STAND_MS))
                {
                    byte energyAmount = ADD_ENERGY_STAND;
                    if (IsWing)
                    {
                        energyAmount = ADD_ENERGY_STAND / 2;
                    }
                    else
                    {
                        if (Action == EntityAction.Sit)
                            energyAmount = ADD_ENERGY_SIT;
                        else if (Action == EntityAction.Lie) energyAmount = ADD_ENERGY_LIE;
                    }

                    QueueAction(() => AddAttributesAsync(ClientUpdateType.Stamina, energyAmount));
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(
                    $"Character::OnTimerAsync() => {Identity}:{Name} Energy recovery: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mXpPoints.ToNextTime()) await ProcXpValAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} XP Points: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mAutoHeal.ToNextTime() && IsAlive)
                    QueueAction(() => AddAttributesAsync(ClientUpdateType.Hitpoints, AUTOHEALLIFE_EACHPERIOD));
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} Auto Heal: {ex.Message}");
                await Log.WriteLogAsync(ex);
            }

            try
            {
                if (mMine.IsActive() && mMine.ToNextTime())
                    await DoMineAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync($"Character::OnTimerAsync() => {Identity}:{Name} DoMineAsync(): {ex.Message}");
                await Log.WriteLogAsync(ex);
            }
        }

        #endregion

        #region Socket

        public DateTime LastLogin => mDbObject.LoginTime;
        public DateTime LastLogout => mDbObject.LogoutTime;
        public int TotalOnlineTime => mDbObject.OnlineSeconds;

        public uint LastDailyUpdate
        {
            get => mDbObject.DayResetDate;
            set => mDbObject.DayResetDate = value;
        }

        public async Task SetLoginAsync()
        {
            mDbObject.LoginTime = mDbObject.LogoutTime = DateTime.Now;
            await SaveAsync();
        }

        public async Task OnDisconnectAsync()
        {
            if (Map?.IsRecordDisable() == false && IsAlive)
            {
                mDbObject.MapID = mIdMap;
                mDbObject.X = mPosX;
                mDbObject.Y = mPosY;
            }

            mDbObject.LogoutTime = DateTime.Now;
            mDbObject.OnlineSeconds += (int) (mDbObject.LogoutTime - mDbObject.LoginTime).TotalSeconds;

            if (!IsAlive)
                mDbObject.HealthPoints = 1;

            try
            {
                if (CurrentEvent is ArenaQualifier qualifier)
                {
                    if (qualifier.IsInsideMatch(Identity))
                    {
                        ArenaQualifier.QualifierMatch match = qualifier.FindMatchByMap(MapIdentity);
                        if (match != null && match.IsRunning) // if not running probably opponent quit first?
                            await match.FinishAsync(null, this, Identity);
                    }
                    else if (qualifier.FindInQueue(Identity) != null)
                    {
                        await qualifier.UnsubscribeAsync(Identity);
                    }
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on leave qualifier disconnection");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                if (Booth != null)
                    await Booth.LeaveMapAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on booth disconnection");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                await NotifyOfflineFriendAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on notifying friends disconnection");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                foreach (Tutor apprentice in m_apprentices.Values.Where(x => x.Student != null))
                {
                    await apprentice.SendTutorAsync();
                    await apprentice.Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, 0, 0);
                }

                if (m_tutorAccess != null)
                    await ServerDbContext.SaveAsync(m_tutorAccess);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on guide dismiss");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                if (Team != null && Team.IsLeader(Identity))
                    await Team.DismissAsync(this, true);
                else if (Team != null)
                    await Team.DismissMemberAsync(this);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on team dismiss");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                if (Trade != null)
                    await Trade.SendCloseAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on close trade");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                await LeaveMapAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on leave map");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                foreach (IStatus status in StatusSet.Status.Values.Where(x => x.Model != null))
                {
                    if (status is StatusMore && status.RemainingTimes == 0)
                        continue;

                    status.Model.LeaveTimes = (uint) status.RemainingTimes;
                    status.Model.RemainTime = (uint) status.RemainingTime;

                    await ServerDbContext.SaveAsync(status.Model);
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on save status");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                await ServerDbContext.SaveAsync(m_monsterKills.Values.ToList());
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on save monster kills");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                await WeaponSkill.SaveAllAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on save weaponskills ");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            try
            {
                if (Syndicate != null && SyndicateMember != null)
                {
                    SyndicateMember.LastLogout = DateTime.Now;
                    await SyndicateMember.SaveAsync();
                }
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on save syndicate");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            await Kernel.BroadcastWorldMsgAsync(new MsgAiPlayerLogout
            {
                Timestamp = Environment.TickCount,
                Id = Identity
            });

            // scope to don't create variable externally
            var msg = new MsgAccServerPlayerExchange
            {
                ServerName = Kernel.GameConfiguration.ServerName
            };
            msg.Data.Add(MsgAccServerPlayerExchange.CreatePlayerData(this));
            await Kernel.AccountServer.SendAsync(msg);

            await Kernel.AccountServer.SendAsync(new MsgAccServerPlayerStatus
            {
                ServerName = Kernel.GameConfiguration.ServerName,
                Status = new List<MsgAccServerPlayerStatus<AccountServer>.PlayerStatus>
                {
                    new()
                    {
                        Identity = Identity,
                        AccountIdentity = Client.AccountIdentity,
                        Online = false,
                        Deleted = m_IsDeleted
                    }
                }
            });

            if (!m_IsDeleted)
            {
                await SaveAsync();
            }

            try
            {
                await ServerDbContext.SaveAsync(new DbGameLoginRecord
                {
                    AccountIdentity = Client.AccountIdentity,
                    UserIdentity = Identity,
                    LoginTime = mDbObject.LoginTime,
                    LogoutTime = mDbObject.LogoutTime,
                    ServerVersion = $"{Kernel.Version}",
                    IpAddress = Client.IpAddress,
                    MacAddress = Client.MacAddress,
                    OnlineTime = (uint) (mDbObject.LogoutTime - mDbObject.LoginTime).TotalSeconds
                });
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Error on saving login rcd");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
            }

            Kernel.Services.Processor.Queue(0, () =>
            {
                RoleManager.ForceLogoutUser(Identity);
                return Task.CompletedTask;
            });
        }

        public override Task SendAsync(IPacket msg)
        {
            try
            {
                return Client.SendAsync(msg);
            }
            catch (Exception ex)
            {
                return Log.WriteLogAsync(LogLevel.Error, ex.Message);
            }
        }

        public override async Task SendSpawnToAsync(Character player)
        {
            await player.SendAsync(new MsgPlayer(this, player, MapX, MapY));

            if (Syndicate != null)
                await Syndicate.SendAsync(player);

            if (FairyType != 0)
                await player.SendAsync(new MsgSuitStatus
                {
                    Action = 1,
                    Data = (int) FairyType,
                    Param = (int) Identity
                });
        }

        public override async Task SendSpawnToAsync(Character player, int x, int y)
        {
            await player.SendAsync(new MsgPlayer(this, player, (ushort) x, (ushort) y));

            if (Syndicate != null)
                await Syndicate.SendAsync(player);

            if (FairyType != 0)
                await player.SendAsync(new MsgSuitStatus
                {
                    Action = 1,
                    Data = (int) FairyType,
                    Param = (int) Identity
                });
        }

        public async Task SendWindowToAsync(Character player)
        {
            await player.SendAsync(new MsgPlayer(this, player)
            {
                WindowSpawn = true
            });
        }

        public async Task BroadcastTeamLifeAsync(bool maxLife = false)
        {
            if (Team != null)
                await Team.BroadcastMemberLifeAsync(this, maxLife);
        }

        #endregion

        #region Database

        public DbCharacter GetDatabaseObject()
        {
            return mDbObject;
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                await using var db = new ServerDbContext();
                db.Update(mDbObject);
                return await Task.FromResult(await db.SaveChangesAsync() != 0);
            }
            catch
            {
                return await Task.FromResult(false);
            }
        }

        #endregion

        #region Deletion

        private bool m_IsDeleted;

        public async Task<bool> DeleteCharacterAsync()
        {
            if (Syndicate != null)
            {
                if (SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                {
                    if (!await Syndicate.QuitSyndicateAsync(this))
                        return false;
                }
                else
                {
                    if (!await Syndicate.DisbandAsync(this))
                        return false;
                }
            }

            await ServerDbContext.ScalarAsync(
                $"INSERT INTO `cq_deluser` SELECT * FROM `cq_user` WHERE `id`={Identity};");
            await ServerDbContext.DeleteAsync(mDbObject);
            await Log.GmLogAsync("delete_user",
                                 $"{Identity},{Name},{MapIdentity},{MapX},{MapY},{Silvers},{ConquerPoints},{Level},{Profession},{FirstProfession},{PreviousProfession}");

            foreach (Friend friend in mFriends.Values)
                await friend.DeleteAsync();

            foreach (Enemy enemy in mEnemies.Values)
                await enemy.DeleteAsync();

            foreach (TradePartner tradePartner in m_tradePartners.Values)
                await tradePartner.DeleteAsync();

            if (Guide != null) await Guide.DeleteAsync();

            DbPeerage peerage = PeerageManager.GetUser(Identity);
            if (peerage != null)
                await ServerDbContext.DeleteAsync(peerage);

            return m_IsDeleted = true;
        }

        #endregion
    }

    /// <summary>Enumeration type for body types for player characters.</summary>
    public enum BodyType : ushort
    {
        AgileMale = 1003,
        MuscularMale = 1004,
        AgileFemale = 2001,
        MuscularFemale = 2002
    }

    /// <summary>Enumeration type for base classes for player characters.</summary>
    public enum BaseClassType : ushort
    {
        Trojan = 10,
        Warrior = 20,
        Archer = 40,
        Ninja = 50,
        Taoist = 100
    }

    public enum PkModeType
    {
        FreePk,
        Peace,
        Team,
        Capture
    }

    public enum RequestType
    {
        Friend,
        Syndicate,
        TeamApply,
        TeamInvite,
        Trade,
        Marriage,
        TradePartner,
        Guide,
        Family,
        CoupleInteraction
    }

    public enum UserFlagType : uint
    {
        None = 0,
        CanClaim = 1 << 0,
        ShowSpecialItems = 1 << 1,
        ClaimGift = 1 << 2,
        OnMeleeAttack = 1 << 3
    }

    [Flags]
    public enum VipFlags
    {
        VipOne = ItemStatusExtraTime | Friends | BlessTime,
        VipTwo = VipOne | BonusLottery | VipFurniture | CityTeleport,
        VipThree = VipTwo | PortalTeleport | CityTeleportTeam,
        VipFour = VipThree | Avatar | DailyQuests | VipHairStyles,
        VipFive = VipFour | FrozenGrotto,
        VipSix = FullVip,

        PortalTeleport = 0x1,
        Avatar = 0x2,
        MoreForVip = 0x4,
        FrozenGrotto = 0x8,
        TeleportTeam = 0x10,
        CityTeleport = 0x20,
        CityTeleportTeam = 0x40,
        BlessTime = 0x80,
        OfflineTrainingGround = 0x100,
        /// <summary>
        /// Refinery and Artifacts
        /// </summary>
        ItemStatusExtraTime = 0x200,
        Friends = 0x400,
        VipHairStyles = 0x800,
        Labirint = 0x1000,
        DailyQuests = 0x2000,
        VipFurniture = 0x4000,
        BonusLottery = 0x8000,

        FullVip = PortalTeleport | Avatar | MoreForVip | FrozenGrotto | TeleportTeam
                  | CityTeleport | CityTeleportTeam | BlessTime | OfflineTrainingGround | ItemStatusExtraTime
                  | Friends | VipHairStyles | Labirint | DailyQuests | VipFurniture | BonusLottery,
        None = 0
    }

    public enum PlayerCountry
    {
        UnitedArabEmirates = 1,
        Argentine,
        Australia,
        Belgium,
        Brazil,
        Canada,
        China,
        Colombia,
        CostaRica,
        CzechRepublic,
        Conquer,
        Germany,
        Denmark,
        DominicanRepublic,
        Egypt,
        Spain,
        Estland,
        Finland,
        France,
        UnitedKingdom,
        HongKong,
        Indonesia,
        India,
        Israel,
        Italy,
        Japan,
        Kuwait,
        SriLanka,
        Lithuania,
        Mexico,
        Macedonia,
        Malaysia,
        Netherlands,
        Norway,
        NewZealand,
        Peru,
        Philippines,
        Poland,
        PuertoRico,
        Portugal,
        Palestine,
        Qatar,
        Romania,
        Russia,
        SaudiArabia,
        Singapore,
        Sweden,
        Thailand,
        Turkey,
        UnitedStates,
        Venezuela,
        Vietnam = 52
    }
}