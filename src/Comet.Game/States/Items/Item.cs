using System;
using System.Text;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Packets;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;
using Comet.Shared;
using Newtonsoft.Json;
using static Comet.Game.States.Items.MapItem;

namespace Comet.Game.States.Items
{
    public class Item
    {
        private const int SpecialFlagSuspicious = 0x1;

        private readonly Character mUser;
        private DbItem mDbItem;
        private DbItemAddition mDbItemAddition;

        public Item()
        {
        }

        public Item(Character user)
        {
            mUser = user;
        }

        public async Task<bool> CreateAsync(DbItemtype type, ItemPosition position = ItemPosition.Inventory)
        {
            if (type == null)
                return false;

            mDbItem = new DbItem
            {
                PlayerId = mUser.Identity,
                Type = type.Type,
                Position = (byte) position,
                Amount = type.Amount,
                AmountLimit = type.AmountLimit,
                Magic1 = (byte) type.Magic1,
                Magic2 = type.Magic2,
                Magic3 = type.Magic3,
                Color = 3,
                Monopoly = 0
            };

            Itemtype = type;
            mDbItemAddition = ItemManager.GetItemAddition(Type, Plus);

            return await SaveAsync() && (mUser.LastAddItemIdentity = Identity) != 0;
        }

        public async Task<bool> CreateAsync(DbItem item)
        {
            if (item == null)
                return false;

            mDbItem = item;
            Itemtype = ItemManager.GetItemtype(item.Type);

            if (Itemtype == null)
                return false;

            mDbItemAddition = ItemManager.GetItemAddition(item.Type, item.Magic3);

            if (mDbItem.Id == 0)
            {
                await SaveAsync();
                mUser.LastAddItemIdentity = Identity;
            }

            return true;
        }

        #region Static

        public static int AdditionPoints(Item item)
        {
            int points = 0;
            for (int i = 0; i < item.Plus; i++)
            {
                points += MsgDataArray.GetAddLevelExp((uint) i, item.IsMount());
            }
            if (item.Plus >= 12 && item.IsMount())
            {
                points += (int) item.CompositionProgress;
            }
            return points;
        }

        public static async Task<MapItemInfo> CreateItemInfoAsync(DbMonstertype monstertype, int quality)
        {
            if (monstertype == null)
                return default;

            int rand;
            if (quality == 0)
            {
                rand = await Kernel.NextAsync(100);
                if (rand >= 0 && rand < 30)
                    quality = 5;
                else if (rand >= 30 && rand < 70)
                    quality = 4;
                else
                    quality = 3;
            }

            rand = await Kernel.NextAsync(1250);
            var itemSort = 0;
            var itemLevel = 0;
            var itemColor = ItemColor.Orange;
            if (rand >= 0 && rand < 20)
            {
                // shoes
                itemSort = 160;
                itemLevel = monstertype.DropShoes;
            }
            else if (rand >= 20 && rand < 50)
            {
                // necklace
                int[] necks =
                {
                    120, 121
                };
                itemSort = necks[await Kernel.NextAsync(necks.Length) % necks.Length];
                itemLevel = monstertype.DropNecklace;
            }
            else if (rand >= 50 && rand < 100)
            {
                // ring
                int[] rings =
                {
                    150, 151, 152
                };
                itemSort = rings[await Kernel.NextAsync(rings.Length) % rings.Length];
                itemLevel = monstertype.DropRing;
            }
            else if (rand >= 100 && rand < 400)
            {
                // armet
                int[] armets =
                {
                    111, 112, 113, 114, 117, 118
                };
                itemSort = armets[await Kernel.NextAsync(armets.Length) % armets.Length];
                itemLevel = monstertype.DropArmet;
            }
            else if (rand >= 400 && rand < 700)
            {
                // armor
                int[] armors =
                {
                    130, 131, 133, 134
                };
                itemSort = armors[await Kernel.NextAsync(armors.Length) % armors.Length];
                itemLevel = monstertype.DropArmet;
            }
            else if (rand >= 700 && rand < 1200)
            {
                // weapon & shield
                rand = await Kernel.NextAsync(100);
                if (rand >= 0 && rand < 20) // backsword
                {
                    itemSort = 421;
                    itemLevel = monstertype.DropWeapon;
                }
                else if (rand >= 20 && rand < 40) // archer
                {
                    itemSort = 500;
                    itemLevel = monstertype.DropWeapon;
                }
                else if (rand >= 40 && rand < 60) // one handed
                {
                    // weapons
                    int[] weapons =
                    {
                        410, 420, 421, 422, 430, 440, 450, 460, 480, 481, 490
                    };
                    itemSort = weapons[await Kernel.NextAsync(weapons.Length) % weapons.Length];
                    itemLevel = monstertype.DropWeapon;
                }
                else if (rand >= 60 && rand < 80) // two handed
                {
                    // weapons
                    int[] weapons =
                    {
                        510, 511, 530, 540, 560, 561, 562, 580
                    };
                    itemSort = weapons[await Kernel.NextAsync(weapons.Length) % weapons.Length];
                    itemLevel = monstertype.DropWeapon;
                }
                else // shield
                {
                    itemSort = 900;
                    itemLevel = monstertype.DropShield;
                    itemColor = (ItemColor) await Kernel.NextAsync(6) + 3;
                }
            }
            else
            {
                if (monstertype.Level < 70)
                    return default;

                // talismans
                // weapons
                int[] talismans =
                {
                    201, 202
                };
                itemSort = talismans[await Kernel.NextAsync(talismans.Length) % talismans.Length];
                itemLevel = 0;
            }

            if (itemLevel == 99)
                return default;

            rand = await Kernel.NextAsync(100);
            if (rand < 50) // down one lev
            {
                int randLev = await Kernel.NextAsync(itemLevel / 2);
                itemLevel = randLev + itemLevel / 3;

                if (itemLevel >= 1)
                    itemLevel--;
            }
            else if (rand >= 80) // up one lev
            {
                if (itemSort >= 110 && itemSort <= 119
                    || itemSort >= 130 && itemSort <= 139
                    || itemSort >= 900 && itemSort <= 999)
                    itemLevel = Math.Min(itemLevel + 1, 9);
                else
                    itemLevel = Math.Min(itemLevel + 1, 23);
            }

            int idItemType = itemSort * 1000 + itemLevel * 10 + quality;
            DbItemtype itemtype = ItemManager.GetItemtype((uint) idItemType);
            if (itemtype == null)
                return default;

            ushort amount;
            var amountLimit = (ushort) Math.Max(1, itemtype.AmountLimit * await Kernel.NextRateAsync(0.3d));
            if (quality > 5)
                amount = (ushort) (amountLimit * (15 + await Kernel.NextAsync(20)) / 100);
            else
                amount = (ushort) (amountLimit * (15 + await Kernel.NextAsync(35)) / 100);

            var socketNum = 0;
            if (itemSort >= 400 && itemSort < 700)
            {
                rand = await Kernel.NextAsync(100);
                if (rand < 5)
                    socketNum = 2;
                else if (rand < 20) socketNum = 1;
            }

            var addition = 0;
            rand = await Kernel.NextAsync(1000);
            if (rand < 8) addition = 1;

            var reduceDamage = 0;
            if (itemSort != 201 && itemSort != 202)
            {
                rand = await Kernel.NextAsync(100);
                if (rand < 3)
                    reduceDamage = 5;
                else if (rand < 15) reduceDamage = 3;
            }

#if DEBUG
            var mapItem = new MapItemInfo
            {
                Type = itemtype.Type,
                Addition = (byte) addition,
                Color = itemColor,
                MaximumDurability = amountLimit,
                Durability = amount,
                ReduceDamage = (byte) reduceDamage,
                SocketNum = (byte) socketNum
            };
            await Log.WriteLogAsync("dropitem", LogLevel.Debug,
                                    $"Dropitem >> {JsonConvert.SerializeObject(mapItem, Formatting.None).Replace("{", "{{").Replace("}", "}}")}");
            return mapItem;
#else
            return new MapItemInfo
            {
                Type = itemtype.Type,
                Addition = (byte)addition,
                Color = itemColor,
                MaximumDurability = amountLimit,
                Durability = amount,
                ReduceDamage = (byte)reduceDamage,
                SocketNum = (byte)socketNum
            };
#endif
        }

        public static int CalculateGemPercentage(SocketGem gem)
        {
            switch (gem)
            {
                case SocketGem.NormalTortoiseGem:
                    return 2;
                case SocketGem.RefinedTortoiseGem:
                    return 4;
                case SocketGem.SuperTortoiseGem:
                    return 6;
                case SocketGem.NormalDragonGem:
                case SocketGem.NormalPhoenixGem:
                case SocketGem.NormalFuryGem:
                    return 5;
                case SocketGem.RefinedDragonGem:
                case SocketGem.RefinedPhoenixGem:
                case SocketGem.NormalRainbowGem:
                case SocketGem.RefinedFuryGem:
                    return 10;
                case SocketGem.SuperDragonGem:
                case SocketGem.SuperPhoenixGem:
                case SocketGem.RefinedRainbowGem:
                case SocketGem.SuperFuryGem:
                case SocketGem.NormalMoonGem:
                    return 15;
                case SocketGem.SuperRainbowGem:
                    return 25;
                case SocketGem.NormalVioletGem:
                case SocketGem.RefinedMoonGem:
                    return 30;
                case SocketGem.RefinedVioletGem:
                case SocketGem.SuperMoonGem:
                case SocketGem.NormalKylinGem:
                    return 50;
                case SocketGem.RefinedKylinGem:
                case SocketGem.SuperVioletGem:
                    return 100;
                case SocketGem.SuperKylinGem:
                    return 200;
                default:
                    return 0;
            }
        }

        public static DbItem CreateEntity(uint type, bool bound = false)
        {
            DbItemtype itemtype = ItemManager.GetItemtype(type);
            if (itemtype == null)
                return null;

            var entity = new DbItem
            {
                Magic1 = (byte) itemtype.Magic1,
                Magic2 = itemtype.Magic2,
                Magic3 = itemtype.Magic3,
                Type = type,
                Amount = itemtype.Amount,
                AmountLimit = itemtype.AmountLimit,
                Gem1 = itemtype.Gem1,
                Gem2 = itemtype.Gem2,
                Monopoly = (byte) (bound ? 3 : 0),
                Color = (byte) ItemColor.Orange
            };
            return entity;
        }

        #endregion

        #region Attributes

        public DbItemtype Itemtype { get; private set; }

        public uint Identity => mDbItem.Id;

        public string Name => Itemtype?.Name ?? Language.StrNone;

        public string FullName
        {
            get
            {
                var builder = new StringBuilder();
                switch (GetQuality())
                {
                    case 9:
                        builder.Append("Super");
                        break;
                    case 8:
                        builder.Append("Elite");
                        break;
                    case 7:
                        builder.Append("Unique");
                        break;
                    case 6:
                        builder.Append("Refined");
                        break;
                }

                builder.Append(Name);
                if (Plus > 0) builder.Append($"(+{Plus})");
                if (SocketOne != SocketGem.NoSocket)
                {
                    if (SocketTwo != SocketGem.NoSocket)
                        builder.Append(" 2-Socketed");
                    else
                        builder.Append(" 1-Socketed");
                }

                if (ReduceDamage > 0) builder.Append($" -{ReduceDamage}%");
                if (Enchantment > 0) builder.Append($" +{Enchantment}HP");
                if (Effect != ItemEffect.None && !IsMount()) builder.Append($" {Effect}");
                return builder.ToString();
            }
        }

        public uint Type => Itemtype?.Type ?? 0;

        /// <summary>
        ///     May be an NPC or Sash ID.
        /// </summary>
        public uint OwnerIdentity
        {
            get => mDbItem.OwnerId;
            set => mDbItem.OwnerId = value;
        }

        /// <summary>
        ///     The current owner of the equipment.
        /// </summary>
        public uint PlayerIdentity
        {
            get => mDbItem.PlayerId;
            set => mDbItem.PlayerId = value;
        }

        public ushort Durability
        {
            get => mDbItem.Amount;
            set => mDbItem.Amount = value;
        }

        public ushort MaximumDurability
        {
            get
            {
                ushort result = mDbItem.AmountLimit;
                switch (SocketOne)
                {
                    case SocketGem.NormalKylinGem:
                    case SocketGem.RefinedKylinGem:
                    case SocketGem.SuperKylinGem:
                        result += (ushort) (OriginalMaximumDurability * (CalculateGemPercentage(SocketOne) / 100.0d));
                        break;
                }

                switch (SocketTwo)
                {
                    case SocketGem.NormalKylinGem:
                    case SocketGem.RefinedKylinGem:
                    case SocketGem.SuperKylinGem:
                        result += (ushort) (OriginalMaximumDurability * (CalculateGemPercentage(SocketTwo) / 100.0d));
                        break;
                }

                return result;
            }
            set => mDbItem.AmountLimit = value;
        }

        public ushort OriginalMaximumDurability => Itemtype.AmountLimit;

        public SocketGem SocketOne
        {
            get => (SocketGem) mDbItem.Gem1;
            set => mDbItem.Gem1 = (byte) value;
        }

        public SocketGem SocketTwo
        {
            get => (SocketGem) mDbItem.Gem2;
            set => mDbItem.Gem2 = (byte) value;
        }

        public ItemPosition Position
        {
            get => (ItemPosition) mDbItem.Position;
            set => mDbItem.Position = (byte) value;
        }

        public byte Plus
        {
            get => (byte) (GetItemSubType() == 730 ? Type % 100 : mDbItem.Magic3);
            set => mDbItem.Magic3 = value;
        }

        public uint SocketProgress
        {
            get => mDbItem.Data;
            set => mDbItem.Data = value;
        }

        public uint CompositionProgress
        {
            get => mDbItem.AddlevelExp;
            set => mDbItem.AddlevelExp = value;
        }

        public ItemEffect Effect
        {
            get => (ItemEffect) mDbItem.Magic1;
            set => mDbItem.Magic1 = (byte) value;
        }

        public byte ReduceDamage
        {
            get => mDbItem.ReduceDmg;
            set => mDbItem.ReduceDmg = value;
        }

        public byte Enchantment
        {
            get => (byte) (Position != ItemPosition.Steed ? mDbItem.AddLife : 0);
            set => mDbItem.AddLife = value;
        }

        public byte AntiMonster
        {
            get => mDbItem.AntiMonster;
            set => mDbItem.AntiMonster = value;
        }

        public ItemColor Color
        {
            get => (ItemColor) mDbItem.Color;
            set => mDbItem.Color = (byte) value;
        }

        public bool IsBound
        {
            get => mDbItem.Monopoly != 0;
            set
            {
                if (value)
                {
                    mDbItem.Monopoly |= ITEM_MONOPOLY_MASK;
                }
                else
                {
                    int monopoly = mDbItem.Monopoly;
                    monopoly &= ~ITEM_MONOPOLY_MASK;
                    mDbItem.Monopoly = (byte) monopoly;
                }
            }
        }

        /// <summary>
        ///     If jar, the amount of monsters killed
        /// </summary>
        public uint Data
        {
            get => mDbItem.Data;
            set => mDbItem.Data = value;
        }

        public uint SyndicateIdentity
        {
            get => mDbItem.Syndicate;
            set => mDbItem.Syndicate = value;
        }

        public uint AccumulateNum
        {
            get => mDbItem.AccumulateNum;
            set => mDbItem.AccumulateNum = value;
        }

        public uint MaxAccumulateNum => Itemtype?.AccumulateLimit ?? 1u;

        #endregion

        #region Requirements

        public int RequiredLevel => Itemtype?.ReqLevel ?? 0;

        public int RequiredProfession => (int) (Itemtype?.ReqProfession ?? 0);

        public int RequiredGender => Itemtype?.ReqSex ?? 0;

        public int RequiredWeaponSkill => Itemtype?.ReqWeaponskill ?? 0;

        public int RequiredForce => Itemtype?.ReqForce ?? 0;

        public int RequiredAgility => Itemtype?.ReqSpeed ?? 0;

        public int RequiredVitality => Itemtype?.ReqHealth ?? 0;

        public int RequiredSpirit => Itemtype?.ReqSoul ?? 0;

        #endregion

        #region Battle Attributes

        public int BattlePower
        {
            get
            {
#if BATTLE_POWER
                if ((!IsEquipment() && !IsMount()) || IsGarment() || IsGourd())
                    return 0;

                int ret = Math.Max(0, (int) Type % 10 - 5);
                if (m_user != null && m_user.Map.IsSkillMap())
                {
                    ret += Math.Max(5, (int) Plus);
                    ret += SocketOne != SocketGem.NoSocket ? 1 : 0;
                    ret += (int) SocketOne % 10 == 3 ? 1 : 0;
                }
                else
                {
                    ret += Plus;
                    ret += SocketOne != SocketGem.NoSocket ? 1 : 0;
                    ret += (int) SocketOne % 10 == 3 ? 1 : 0;
                    ret += SocketTwo != SocketGem.NoSocket ? 1 : 0;
                    ret += (int) SocketTwo % 10 == 3 ? 1 : 0;
                }

                if ((IsBackswordType() || IsWeaponTwoHand()) && (m_user?.UserPackage[ItemPosition.LeftHand] == null || m_user.UserPackage[ItemPosition.LeftHand].IsArrowSort()))
                    ret *= 2;

                return ret;
#else
                return 0;
#endif
            }
        }

        public int Life
        {
            get
            {
                int result = Itemtype?.Life ?? 0;
                result += Enchantment;
                result += mDbItemAddition?.Life ?? 0;
                return result;
            }
        }

        public int Mana => Itemtype?.Mana ?? 0;

        public int MinAttack
        {
            get
            {
                int result = Itemtype?.AttackMin ?? 0;
                result += mDbItemAddition?.AttackMin ?? 0;
                DbWeaponSkill ws = mUser.WeaponSkill[(ushort) GetItemSubType()];
                if (ws != null && ws.Level > 12)
                    result += result * (1 + (Role.MAX_WEAPONSKILLLEVEL - ws.Level) / 100);
                return result;
            }
        }

        public int MaxAttack
        {
            get
            {
                int result = Itemtype?.AttackMax ?? 0;
                if (Position == ItemPosition.LeftHand && !IsShield())
                    result /= 2;
                result += mDbItemAddition?.AttackMax ?? 0;
                DbWeaponSkill ws = mUser.WeaponSkill[(ushort) GetItemSubType()];
                if (ws != null && ws.Level > 12)
                    result += result * (1 + (Role.MAX_WEAPONSKILLLEVEL - ws.Level) / 100);
                return result;
            }
        }

        public int AddFinalDamage
        {
            get
            {
                if (Position == ItemPosition.AttackTalisman)
                {
                    int result = Itemtype?.AttackMax ?? 0;
                    result += mDbItemAddition?.AttackMax ?? 0;
                    return result;
                }

                return 0;
            }
        }

        public int MagicAttack
        {
            get
            {
                int result = Itemtype?.MagicAtk ?? 0;
                result += mDbItemAddition?.MagicAtk ?? 0;
                DbWeaponSkill ws = mUser.WeaponSkill[(ushort) GetItemSubType()];
                if (ws != null && ws.Type == 421 && ws.Level > 12)
                    result += result * (1 + (Role.MAX_WEAPONSKILLLEVEL - ws.Level) / 100);
                return result;
            }
        }

        public int AddFinalMagicDamage
        {
            get
            {
                if (Position == ItemPosition.AttackTalisman)
                {
                    int result = Itemtype?.MagicAtk ?? 0;
                    result += mDbItemAddition?.MagicAtk ?? 0;
                    return result;
                }

                return 0;
            }
        }

        public int Defense
        {
            get
            {
                int result = Itemtype?.Defense ?? 0;
                if (IsArrowSort())
                    return result;
                result += mDbItemAddition?.Defense ?? 0;
                return result;
            }
        }

        public int AddFinalDefense
        {
            get
            {
                if (Position == ItemPosition.DefenceTalisman)
                {
                    int result = Itemtype?.Defense ?? 0;
                    result += mDbItemAddition?.Defense ?? 0;
                    return result;
                }

                return 0;
            }
        }

        public int MagicDefense
        {
            get
            {
                if (Position == ItemPosition.Armor || Position == ItemPosition.Headwear ||
                    Position == ItemPosition.Necklace) return mDbItemAddition?.MagicDef ?? 0;

                return Itemtype?.MagicDef ?? 0;
            }
        }

        public int AddFinalMagicDefense
        {
            get
            {
                if (Position == ItemPosition.DefenceTalisman)
                {
                    int result = Itemtype?.MagicDef ?? 0;
                    result += mDbItemAddition?.MagicDef ?? 0;
                    return result;
                }

                return 0;
            }
        }

        public int MagicDefenseBonus
        {
            get
            {
                if (Position == ItemPosition.Armor || Position == ItemPosition.Headwear) return Itemtype?.MagicDef ?? 0;

                return 0;
            }
        }

        public int Accuracy
        {
            get
            {
                if (Position == ItemPosition.Steed)
                    return 0;

                int result = Itemtype?.Dexterity ?? 0;
                result += mDbItemAddition?.Dexterity ?? 0;
                if (IsWeaponTwoHand())
                    result *= 2;
                if (IsBow())
                    result *= 2;
                return result;
            }
        }

        public int Dodge
        {
            get
            {
                int result = Itemtype?.Dodge ?? 0;
                result += mDbItemAddition?.Dodge ?? 0;
                return result;
            }
        }

        public int Blessing => Position == ItemPosition.Steed ? 0 : mDbItem?.ReduceDmg ?? 0;

        public int DragonGemEffect
        {
            get
            {
                var result = 0;
                switch (SocketOne)
                {
                    case SocketGem.NormalDragonGem:
                    case SocketGem.RefinedDragonGem:
                    case SocketGem.SuperDragonGem:
                        result += CalculateGemPercentage(SocketOne);
                        break;
                }

                switch (SocketTwo)
                {
                    case SocketGem.NormalDragonGem:
                    case SocketGem.RefinedDragonGem:
                    case SocketGem.SuperDragonGem:
                        result += CalculateGemPercentage(SocketTwo);
                        break;
                }

                return result;
            }
        }

        public int PhoenixGemEffect
        {
            get
            {
                var result = 0;
                switch (SocketOne)
                {
                    case SocketGem.NormalPhoenixGem:
                    case SocketGem.RefinedPhoenixGem:
                    case SocketGem.SuperPhoenixGem:
                        result += CalculateGemPercentage(SocketOne);
                        break;
                }

                switch (SocketTwo)
                {
                    case SocketGem.NormalPhoenixGem:
                    case SocketGem.RefinedPhoenixGem:
                    case SocketGem.SuperPhoenixGem:
                        result += CalculateGemPercentage(SocketTwo);
                        break;
                }

                return result;
            }
        }

        public int RainbowGemEffect
        {
            get
            {
                var result = 0;
                switch (SocketOne)
                {
                    case SocketGem.NormalRainbowGem:
                    case SocketGem.RefinedRainbowGem:
                    case SocketGem.SuperRainbowGem:
                        result += CalculateGemPercentage(SocketOne);
                        break;
                }

                switch (SocketTwo)
                {
                    case SocketGem.NormalRainbowGem:
                    case SocketGem.RefinedRainbowGem:
                    case SocketGem.SuperRainbowGem:
                        result += CalculateGemPercentage(SocketTwo);
                        break;
                }

                return result;
            }
        }

        public int VioletGemEffect
        {
            get
            {
                var result = 0;
                switch (SocketOne)
                {
                    case SocketGem.NormalVioletGem:
                    case SocketGem.RefinedVioletGem:
                    case SocketGem.SuperVioletGem:
                        result += CalculateGemPercentage(SocketOne);
                        break;
                }

                switch (SocketTwo)
                {
                    case SocketGem.NormalVioletGem:
                    case SocketGem.RefinedVioletGem:
                    case SocketGem.SuperVioletGem:
                        result += CalculateGemPercentage(SocketTwo);
                        break;
                }

                return result;
            }
        }

        public int FuryGemEffect
        {
            get
            {
                var result = 0;
                switch (SocketOne)
                {
                    case SocketGem.NormalFuryGem:
                    case SocketGem.RefinedFuryGem:
                    case SocketGem.SuperFuryGem:
                        result += CalculateGemPercentage(SocketOne);
                        break;
                }

                switch (SocketTwo)
                {
                    case SocketGem.NormalFuryGem:
                    case SocketGem.RefinedFuryGem:
                    case SocketGem.SuperFuryGem:
                        result += CalculateGemPercentage(SocketTwo);
                        break;
                }

                return result;
            }
        }

        public int MoonGemEffect
        {
            get
            {
                var result = 0;
                switch (SocketOne)
                {
                    case SocketGem.NormalMoonGem:
                    case SocketGem.RefinedMoonGem:
                    case SocketGem.SuperMoonGem:
                        result += CalculateGemPercentage(SocketOne);
                        break;
                }

                switch (SocketTwo)
                {
                    case SocketGem.NormalMoonGem:
                    case SocketGem.RefinedMoonGem:
                    case SocketGem.SuperMoonGem:
                        result += CalculateGemPercentage(SocketTwo);
                        break;
                }

                return result;
            }
        }

        public int TortoiseGemEffect
        {
            get
            {
                var result = 0;
                switch (SocketOne)
                {
                    case SocketGem.NormalTortoiseGem:
                    case SocketGem.RefinedTortoiseGem:
                    case SocketGem.SuperTortoiseGem:
                        result += CalculateGemPercentage(SocketOne);
                        break;
                }

                switch (SocketTwo)
                {
                    case SocketGem.NormalTortoiseGem:
                    case SocketGem.RefinedTortoiseGem:
                    case SocketGem.SuperTortoiseGem:
                        result += CalculateGemPercentage(SocketTwo);
                        break;
                }

                return result;
            }
        }

        public int AttackRange => Itemtype?.AtkRange ?? 1;

        public int Vigor
        {
            get
            {
                int result = Itemtype.Dexterity;
                result += mDbItemAddition?.Dexterity ?? 0;
                return result;
            }
        }

        #endregion

        #region Change Data

        public async Task ChangeOwnerAsync(uint idNewOwner, ChangeOwnerType type)
        {
            await ServerDbContext.SaveAsync(new DbItemOwnerHistory
            {
                OldOwnerIdentity = PlayerIdentity,
                NewOwnerIdentity = idNewOwner,
                Operation = (byte) type,
                Time = DateTime.Now,
                ItemIdentity = Identity
            });

            PlayerIdentity = idNewOwner;
            await SaveAsync();
        }

        public async Task<bool> ChangeTypeAsync(uint newType)
        {
            DbItemtype itemtype = ItemManager.GetItemtype(newType);
            if (itemtype == null)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"ChangeType() Invalid itemtype id {newType}");
                return false;
            }

            mDbItem.Type = itemtype.Type;
            Itemtype = itemtype;

            mDbItemAddition = ItemManager.GetItemAddition(newType, mDbItem.Magic3);
            await mUser.SendAsync(new MsgItemInfo(this, MsgItemInfo<Client>.ItemMode.Update));
            await SaveAsync();
            return true;
        }

        public bool ChangeAddition(int level = -1)
        {
            if (level < 0)
                level = (byte) (Plus + 1);

            DbItemAddition add = null;
            if (level > 0)
            {
                uint type = Type;
                add = ItemManager.GetItemAddition(type, (byte) level);
                if (add == null)
                    return false;
            }

            Plus = (byte) level;
            mDbItemAddition = add;
            return true;
        }

        #endregion

        #region Update And Upgrade

        public bool GetUpLevelChance(out double chance, out int nextId)
        {
            nextId = 0;
            chance = 0;
            int sort = (int) (Type / 10000), subtype = (int) (Type / 1000);

            DbItemtype info = NextItemLevel((int) Type);
            if (info == null)
                return false;

            nextId = (int) info.Type;

            if (info.ReqLevel >= 120)
                return false;

            chance = 100.00;
            if (sort == 11 || sort == 14 || sort == 13 || sort == 90 || subtype == 123) //Head || Armor || Shield
            {
                switch ((int) (info.Type % 100 / 10))
                {
                    //case 5:
                    //    nChance = 50.00;
                    //    break;
                    case 6:
                        chance = 40.00;
                        break;
                    case 7:
                        chance = 30.00;
                        break;
                    case 8:
                        chance = 20.00;
                        break;
                    case 9:
                        chance = 15.00;
                        break;
                    default:
                        chance = 500.00;
                        break;
                }

                switch (info.Type % 10)
                {
                    case 6:
                        chance = chance * 0.90;
                        break;
                    case 7:
                        chance = chance * 0.70;
                        break;
                    case 8:
                        chance = chance * 0.30;
                        break;
                    case 9:
                        chance = chance * 0.10;
                        break;
                }
            }
            else
            {
                switch ((int) (info.Type % 1000 / 10))
                {
                    //case 11:
                    //    nChance = 95.00;
                    //    break;
                    case 12:
                        chance = 90.00;
                        break;
                    case 13:
                        chance = 85.00;
                        break;
                    case 14:
                        chance = 80.00;
                        break;
                    case 15:
                        chance = 75.00;
                        break;
                    case 16:
                        chance = 70.00;
                        break;
                    case 17:
                        chance = 65.00;
                        break;
                    case 18:
                        chance = 60.00;
                        break;
                    case 19:
                        chance = 55.00;
                        break;
                    case 20:
                        chance = 50.00;
                        break;
                    case 21:
                        chance = 45.00;
                        break;
                    case 22:
                        chance = 40.00;
                        break;
                    default:
                        chance = 500.00;
                        break;
                }

                switch (info.Type % 10)
                {
                    case 6:
                        chance = chance * 0.90;
                        break;
                    case 7:
                        chance = chance * 0.70;
                        break;
                    case 8:
                        chance = chance * 0.30;
                        break;
                    case 9:
                        chance = chance * 0.10;
                        break;
                }
            }

            return true;
        }

        public DbItemtype NextItemLevel()
        {
            return NextItemLevel((int) Type);
        }

        public DbItemtype NextItemLevel(int id)
        {
            // By CptSky
            int nextId = id;

            var sort = (byte) (id / 100000);
            var type = (byte) (id / 10000);
            var subType = (short) (id / 1000);

            if (sort == 1) //!Weapon
            {
                if (type == 12 && (subType == 120 || subType == 121) || type == 15 || type == 16
                   ) //Necklace || Ring || Boots
                {
                    var level = (byte) ((id % 1000 - id % 10) / 10);
                    if (type == 12 && level < 8 || type == 15 && subType != 152 && level > 0 && level < 21 ||
                        type == 15 && subType == 152 && level >= 4 && level < 22 ||
                        type == 16 && level > 0 && level < 21)
                    {
                        //Check if it's still the same type of item...
                        if ((short) ((nextId + 20) / 1000) == subType)
                            nextId += 20;
                    }
                    else if (type == 12 && level == 8 || type == 12 && level >= 21 ||
                             type == 15 && subType != 152 && level == 0
                             || type == 15 && subType != 152 && level >= 21 ||
                             type == 15 && subType == 152 && level >= 22 || type == 16 && level >= 21)
                    {
                        //Check if it's still the same type of item...
                        if ((short) ((nextId + 10) / 1000) == subType)
                            nextId += 10;
                    }
                    else if (type == 12 && level >= 9 && level < 21 || type == 15 && subType == 152 && level == 1)
                    {
                        //Check if it's still the same type of item...
                        if ((short) ((nextId + 30) / 1000) == subType)
                            nextId += 30;
                    }
                }
                else
                {
                    var Quality = (byte) (id % 10);
                    if (type == 11 || type == 14 || type == 13 || subType == 123) //Head || Armor
                    {
                        var level = (byte) ((id % 100 - id % 10) / 10);

                        //Check if it's still the same type of item...
                        if ((short) ((nextId + 10) / 1000) == subType)
                            nextId += 10;
                    }
                }
            }
            else if (sort == 4 || sort == 5 || sort == 6) //Weapon
            {
                //Check if it's still the same type of item...
                if ((short) ((nextId + 10) / 1000) == subType)
                    nextId += 10;

                //Invalid Backsword ID
                if (nextId / 10 == 42103 || nextId / 10 == 42105 || nextId / 10 == 42109 || nextId / 10 == 42111)
                    nextId += 10;
            }
            else if (sort == 9)
            {
                var Level = (byte) ((id % 100 - id % 10) / 10);
                if (Level != 30) //!Max...
                    //Check if it's still the same type of item...
                    if ((short) ((nextId + 10) / 1000) == subType)
                        nextId += 10;
            }

            return ItemManager.GetItemtype((uint) nextId);
        }

        public uint ChkUpEqQuality(uint type)
        {
            if (type == TYPE_MOUNT_ID)
                return 0;

            uint nQuality = type % 10;

            if (nQuality < 3 || nQuality >= 9)
                return 0;

            nQuality = Math.Min(9, Math.Max(6, ++nQuality));

            type = type - type % 10 + nQuality;

            return ItemManager.GetItemtype(type)?.Type ?? 0;
        }

        public bool GetUpEpQualityInfo(out double nChance, out uint idNewType)
        {
            nChance = 0;
            idNewType = 0;

            if (Type == 150000 || Type == 150310 || Type == 150320 || Type == 410301 || Type == 421301 ||
                Type == 500301)
                return false;

            idNewType = ChkUpEqQuality(Type);
            nChance = 100;

            switch (Type % 10)
            {
                case 6:
                    nChance = 50;
                    break;
                case 7:
                    nChance = 33;
                    break;
                case 8:
                    nChance = 20;
                    break;
                default:
                    nChance = 100;
                    break;
            }

            DbItemtype itemtype = ItemManager.GetItemtype(idNewType);
            if (itemtype == null)
                return false;

            uint nFactor = itemtype.ReqLevel;

            if (nFactor > 70)
                nChance = (uint) (nChance * (100 - (nFactor - 70) * 1.0) / 100);

            nChance = Math.Max(1, nChance);
            return true;
        }

        public uint GetFirstId()
        {
            uint firstId = Type;

            var sort = (byte) (Type / 100000);
            var type = (byte) (Type / 10000);
            var subType = (short) (Type / 1000);

            if (Type is 150000 or 150310 or 150320 or 410301 or 421301 or 500301 or 601301 or 610301)
                return Type;

            if (Type is >= 120310 and <= 120319)
                return Type;

            if (sort == 1) //!Weapon
            {
                if (subType is 120 or 121) // Necklace
                {
                    firstId = Type - Type % 1000 + Type % 10;
                }
                else if (type is 15 or 16) // Ring || Boots
                {
                    firstId = Type - Type % 1000 + 10 + Type % 10;
                }
                else if (type == 11 || subType is 114 or 123 || type == 14) //Head
                {
                    firstId = Type - Type % 1000 + Type % 10;
                }
                else if (type == 13) // Armor
                {
                    firstId = Type - Type % 1000 + Type % 10;
                }
            }
            else if (sort is 4 or 5 or 6) // Weapon
            {
                firstId = Type - Type % 1000 + 20 + Type % 10;
            }
            else if (sort == 9)
            {
                firstId = Type - Type % 1000 + Type % 10;
            }

            return ItemManager.GetItemtype(firstId)?.Type ?? Type;
        }

        public uint GetUpQualityGemAmount()
        {
            if (!GetUpEpQualityInfo(out double nChance, out _))
                return 0;
            return (uint) (100 / nChance + 1) * 12 / 10;
        }

        public uint GetUpgradeGemAmount()
        {
            if (!GetUpLevelChance(out double nChance, out _))
                return 0;
            return (uint) Math.Max(3, (100 / nChance + 1) * 12 / 10);
        }

        public async Task<bool> DegradeItemAsync(bool bCheckDura = true)
        {
            if (!IsEquipment())
                return false;
            if (bCheckDura)
                if (Durability / 100 < MaximumDurability / 100)
                {
                    await mUser.SendAsync(Language.StrItemErrRepairItem);
                    return false;
                }

            uint newId = GetFirstId();
            DbItemtype newType = ItemManager.GetItemtype(newId);
            if (newType == null || newType.Type == Type)
                return false;
            return await ChangeTypeAsync(newType.Type);
        }

        public async Task<bool> UpItemQualityAsync()
        {
            if (Durability / 100 < MaximumDurability / 100)
            {
                await mUser.SendAsync(Language.StrItemErrRepairItem);
                return false;
            }

            if (!GetUpEpQualityInfo(out double nChance, out uint newId))
            {
                await mUser.SendAsync(Language.StrItemErrMaxQuality);
                return false;
            }

            DbItemtype newType = ItemManager.GetItemtype(newId);
            if (newType == null)
            {
                await mUser.SendAsync(Language.StrItemErrMaxLevel);
                return false;
            }

            int gemCost = (int) (100 / nChance + 1) * 12 / 10;

            if (!await mUser.UserPackage.SpendDragonBallsAsync(gemCost, IsBound))
            {
                await mUser.SendAsync(string.Format(Language.StrItemErrNotEnoughDragonBalls, gemCost));
                return false;
            }

            if (await Kernel.ChanceCalcAsync(0.5d))
            {
                if (SocketOne == SocketGem.NoSocket)
                    SocketOne = SocketGem.EmptySocket;
                else if (SocketTwo == SocketGem.NoSocket)
                    SocketTwo = SocketGem.EmptySocket;
            }

            return await ChangeTypeAsync(newType.Type);
        }

        /// <summary>
        ///     This method will upgrade an equipment level using meteors.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> UpEquipmentLevelAsync()
        {
            if (Durability / 100 < MaximumDurability / 100)
            {
                await mUser.SendAsync(Language.StrItemErrRepairItem);
                return false;
            }

            if (!GetUpLevelChance(out double nChance, out int newId))
            {
                await mUser.SendAsync(Language.StrItemErrMaxLevel);
                return false;
            }


            DbItemtype newType = ItemManager.GetItemtype((uint) newId);
            if (newType == null)
            {
                await mUser.SendAsync(Language.StrItemErrMaxLevel);
                return false;
            }

            if (newType.ReqLevel > mUser.Level)
            {
                await mUser.SendAsync(Language.StrItemErrNotEnoughLevel);
                return false;
            }

            int gemCost = (int) (100 / nChance + 1) * 12 / 10;
            if (!await mUser.UserPackage.SpendMeteorsAsync(gemCost))
            {
                await mUser.SendAsync(string.Format(Language.StrItemErrNotEnoughMeteors, gemCost));
                return false;
            }

            if (await Kernel.ChanceCalcAsync(0.5d))
            {
                if (SocketOne == SocketGem.NoSocket)
                    SocketOne = SocketGem.EmptySocket;
                else if (SocketTwo == SocketGem.NoSocket)
                    SocketTwo = SocketGem.EmptySocket;
            }

            Durability = newType.AmountLimit;
            MaximumDurability = newType.AmountLimit;
            return await ChangeTypeAsync(newType.Type);
        }

        public async Task<bool> UpUltraEquipmentLevelAsync()
        {
            if (Durability / 100 < MaximumDurability / 100)
            {
                await mUser.SendAsync(Language.StrItemErrRepairItem);
                return false;
            }

            DbItemtype newType = NextItemLevel((int) Type);

            if (newType == null || newType.Type == Type)
            {
                await mUser.SendAsync(Language.StrItemErrMaxLevel);
                return false;
            }

            if (newType.ReqLevel > mUser.Level)
            {
                await mUser.SendAsync(Language.StrItemErrNotEnoughLevel);
                return false;
            }

            Durability = MaximumDurability;
            return await ChangeTypeAsync(newType.Type);
        }

        public int GetRecoverDurCost()
        {
            if (Durability > 0 && Durability < OriginalMaximumDurability)
            {
                var price = (int) Itemtype.Price;
                double qualityMultiplier = 0;

                switch (Type % 10)
                {
                    case 9:
                        qualityMultiplier = 1.125;
                        break;
                    case 8:
                        qualityMultiplier = 0.975;
                        break;
                    case 7:
                        qualityMultiplier = 0.9;
                        break;
                    case 6:
                        qualityMultiplier = 0.825;
                        break;
                    default:
                        qualityMultiplier = 0.75;
                        break;
                }

                return (int) Math.Ceiling(
                    price * ((OriginalMaximumDurability - Durability) / (float) OriginalMaximumDurability) *
                    qualityMultiplier);
            }

            return 0;
        }

        public async Task<bool> RecoverDurabilityAsync()
        {
            MaximumDurability = OriginalMaximumDurability;
            await mUser.SendAsync(new MsgItemInfo(this, MsgItemInfo<Client>.ItemMode.Update));
            await SaveAsync();
            return true;
        }

        #endregion

        #region Durability

        public async Task RepairItemAsync()
        {
            if (mUser == null)
                return;

            if (!IsEquipment() && !IsWeapon())
                return;

            if (IsBroken())
            {
                if (!await mUser.UserPackage.SpendMeteorsAsync(5))
                {
                    await mUser.SendAsync(Language.StrItemErrRepairMeteor);
                    return;
                }

                Durability = MaximumDurability;
                await SaveAsync();
                await mUser.SendAsync(new MsgItemInfo(this, MsgItemInfo<Client>.ItemMode.Update));
                await Log.GmLogAsync(
                    "Repair",
                    string.Format("User [{2}] repaired broken [{0}][{1}] with 5 meteors.", Type, Identity,
                                  PlayerIdentity));
                return;
            }

            var nRecoverDurability = (ushort) (Math.Max(0u, MaximumDurability) - Durability);
            if (nRecoverDurability == 0)
                return;

            int nRepairCost = GetRecoverDurCost() - 1;

            if (!await mUser.SpendMoneyAsync(Math.Max(1, nRepairCost), true))
                return;

            Durability = MaximumDurability;
            await SaveAsync();
            await mUser.SendAsync(new MsgItemInfo(this, MsgItemInfo<Client>.ItemMode.Update));
            await Log.GmLogAsync(
                "Repair",
                string.Format("User [{2}] repaired broken [{0}][{1}] with {3} silvers.", Type, Identity, PlayerIdentity,
                              nRepairCost));
        }

        #endregion

        #region Equip Lock

        public async Task<bool> TryUnlockAsync()
        {
            if (HasUnlocked())
            {
                await mUser.SendAsync(new MsgEquipLock
                                           {Action = MsgEquipLock<Client>.LockMode.UnlockedItem, Identity = Identity});
                await mUser.SendAsync(new MsgEquipLock
                                           {Action = MsgEquipLock<Client>.LockMode.RequestUnlock, Identity = Identity});

                await DoUnlockAsync();
                return true;
            }

            if (IsUnlocking())
            {
                await mUser.SendAsync(new MsgEquipLock
                {
                    Action = MsgEquipLock<Client>.LockMode.RequestUnlock,
                    Identity = Identity,
                    Mode = 3,
                    Param = (uint) (mDbItem.Plunder.Value.Year * 10000 + mDbItem.Plunder.Value.Day * 100 +
                                    mDbItem.Plunder.Value.Month)
                });
                return false;
            }

            return true;
        }

        public Task SetLockAsync()
        {
            mDbItem.Plunder = DateTime.MinValue;
            return SaveAsync();
        }

        public Task SetUnlockAsync()
        {
            mDbItem.Plunder = DateTime.Now.AddDays(3);
            return SaveAsync();
        }

        public Task DoUnlockAsync()
        {
            mDbItem.Plunder = null;
            return SaveAsync();
        }

        public bool HasUnlocked()
        {
            return mDbItem.Plunder != null && mDbItem.Plunder != DateTime.MinValue &&
                   mDbItem.Plunder <= DateTime.Now;
        }

        public bool IsLocked()
        {
            return mDbItem.Plunder != null;
        }

        public bool IsUnlocking()
        {
            return mDbItem.Plunder != null && mDbItem.Plunder != DateTime.MinValue && mDbItem.Plunder > DateTime.Now;
        }

        #endregion

        #region Query info

        public static bool IsEatEnable(uint type)
        {
            return IsMedicine(type);
        }

        public bool IsEatEnable()
        {
            return IsEatEnable(Type);
        }

        public uint CalculateSocketProgress()
        {
            uint total = 0;
            total += TALISMAN_SOCKET_QUALITY_ADDITION[Type % 10];
            total += TALISMAN_SOCKET_PLUS_ADDITION[Plus];
            if (IsWeapon())
            {
                if (SocketTwo > 0)
                    total += TALISMAN_SOCKET_HOLE_ADDITION0[2];
                else if (SocketOne > 0)
                    total += TALISMAN_SOCKET_HOLE_ADDITION0[1];
            }
            else
            {
                if (SocketTwo > 0)
                    total += TALISMAN_SOCKET_HOLE_ADDITION1[2];
                else if (SocketOne > 0)
                    total += TALISMAN_SOCKET_HOLE_ADDITION1[1];
            }

            return total;
        }

        public bool IsCountable()
        {
            return MaxAccumulateNum > 1;
        }

        public bool IsPileEnable()
        {
            return IsExpend() && MaxAccumulateNum > 1;
        }

        public bool IsBroken()
        {
            return Durability == 0;
        }

        public int GetSellPrice()
        {
            if (IsBroken() || IsArrowSort() || IsBound || IsLocked())
                return 0;

            int price = (int) (Itemtype?.Price ?? 0) / 3 * Durability / MaximumDurability;
            return price;
        }

        public bool IsGem()
        {
            return GetItemSubType() == 700;
        }

        public bool IsNonsuchItem()
        {
            switch (Type)
            {
                case TYPE_DRAGONBALL:
                case TYPE_METEOR:
                case TYPE_METEORTEAR:
                    return true;
            }

            // precious gem
            if (IsGem() && Type % 10 >= 2)
                return true;

            // todo handle chests inside of inventory

            // other type
            if (GetItemSort() == ItemSort.ItemsortUsable
                || GetItemSort() == ItemSort.ItemsortUsable2
                || GetItemSort() == ItemSort.ItemsortUsable3)
                return false;

            // high quality
            if (GetQuality() >= 8)
                return true;

            int nGem1 = (int) SocketOne % 10;
            int nGem2 = (int) SocketTwo % 10;

            var isNonSuchItem = false;

            if (IsWeapon())
            {
                if (SocketOne != SocketGem.EmptySocket && nGem1 >= 2
                    || SocketTwo != SocketGem.EmptySocket && nGem2 >= 2)
                    isNonSuchItem = true;
            }
            else if (IsShield())
            {
                if (SocketOne != SocketGem.NoSocket || SocketTwo != SocketGem.NoSocket)
                    isNonSuchItem = true;
            }

            return isNonSuchItem;
        }

        public bool IsSuspicious()
        {
            return (mDbItem.Specialflag & SpecialFlagSuspicious) != 0;
        }

        public bool IsMonopoly()
        {
            return (Itemtype.Monopoly & ITEM_MONOPOLY_MASK) != 0;
        }

        public bool IsNeverDropWhenDead()
        {
            return (Itemtype.Monopoly & ITEM_NEVER_DROP_WHEN_DEAD_MASK) != 0 || IsMonopoly() || Plus > 5 || IsLocked();
        }

        public bool IsDisappearWhenDropped()
        {
            return IsMonopoly() || IsBound;
        }

        public bool CanBeDropped()
        {
            return !IsMonopoly() && !IsLocked() && !IsSuspicious() && BattlePower < 8;
        }

        public bool CanBeStored()
        {
            return (Itemtype.Monopoly & ITEM_STORAGE_MASK) == 0;
        }

        public bool IsHoldEnable()
        {
            return IsWeaponOneHand() || IsWeaponTwoHand() || IsWeaponProBased() || IsBow() || IsShield() ||
                   IsArrowSort();
        }

        public bool IsBow()
        {
            return IsBow(Type);
        }

        public ItemPosition GetPosition()
        {
            if (IsHelmet())
                return ItemPosition.Headwear;
            if (IsNeck())
                return ItemPosition.Necklace;
            if (IsRing())
                return ItemPosition.Ring;
            if (IsBangle())
                return ItemPosition.Ring;
            if (IsWeapon())
                return ItemPosition.RightHand;
            if (IsShield())
                return ItemPosition.LeftHand;
            if (IsArrowSort())
                return ItemPosition.LeftHand;
            if (IsArmor())
                return ItemPosition.Armor;
            if (IsShoes())
                return ItemPosition.Boots;
            if (IsGourd())
                return ItemPosition.Gourd;
            if (IsGarment())
                return ItemPosition.Garment;
            return ItemPosition.Inventory;
        }

        public bool IsArmor()
        {
            return IsArmor(Type);
        }

        public static bool IsArmor(uint type)
        {
            return type / 10000 == 13;
        }

        public bool IsMedicine()
        {
            return IsMedicine(Type);
        }

        public static bool IsMedicine(uint type)
        {
            return GetItemtype(type) == ITEMTYPE_PHYSIC;
        }

        public bool IsEquipEnable()
        {
            return IsEquipment() || IsArrowSort() || IsGourd() || IsGarment() || IsTalisman() || IsMount();
        }

        public bool IsBackswordType()
        {
            return GetItemtype() == 421;
        }

        public int GetItemtype()
        {
            return GetItemtype(Type);
        }

        public bool IsEquipment()
        {
            return IsHelmet() || IsNeck() || IsRing() || IsBangle() || IsWeapon() || IsArmor() || IsShoes() ||
                   IsShield() || IsTalisman();
        }

        public static bool IsMount(uint type)
        {
            return type == 300000;
        }

        public bool IsMount()
        {
            return IsMount(Type);
        }

        public static bool IsTalisman(uint type)
        {
            return IsAttackTalisman(type) || IsDefenseTalisman(type) || IsCrop(type);
        }

        public bool IsTalisman()
        {
            return IsTalisman(Type);
        }

        public static bool IsAttackTalisman(uint type)
        {
            return type >= 201000 && type < 202000;
        }

        public bool IsAttackTalisman()
        {
            return IsAttackTalisman(Type);
        }

        public static bool IsDefenseTalisman(uint type)
        {
            return type >= 202000 && type < 203000;
        }

        public bool IsDefenseTalisman()
        {
            return IsDefenseTalisman(Type);
        }

        public static bool IsCrop(uint type)
        {
            return type >= 203000 && type < 204000;
        }

        public bool IsCrop()
        {
            return IsCrop(Type);
        }

        public int GetItemSubType()
        {
            return GetItemSubType(Type);
        }

        public ItemSort GetItemSort()
        {
            return GetItemSort(Type);
        }

        public bool IsArrowSort()
        {
            return IsArrowSort(Type);
        }

        public bool IsHelmet()
        {
            return IsHelmet(Type);
        }

        public static bool IsHelmet(uint type)
        {
            return type >= 110000 && type < 120000 || type >= 140000 && type < 150000 ||
                   type >= 123000 && type < 124000;
        }

        public bool IsNeck()
        {
            return IsNeck(Type);
        }

        public static bool IsNeck(uint type)
        {
            return type >= 120000 && type < 123000;
        }

        public bool IsRing()
        {
            return IsRing(Type);
        }

        public static bool IsRing(uint type)
        {
            return type >= 150000 && type < 152000;
        }

        public bool IsBangle()
        {
            return IsBangle(Type);
        }

        public static bool IsBangle(uint type)
        {
            return type >= 152000 && type < 153000;
        }

        public bool IsShoes()
        {
            return IsShoes(Type);
        }

        public static bool IsShoes(uint type)
        {
            return type >= 160000 && type < 161000;
        }

        public bool IsGourd()
        {
            return Type >= 2100000 && Type < 2200000;
        }

        public bool IsGarment()
        {
            return Type >= 170000 && Type < 200000;
        }

        public bool IsWeaponOneHand()
        {
            return IsWeaponOneHand(Type);
        } // single hand use

        public static bool IsWeaponOneHand(uint type)
        {
            return GetItemSort(type) == ItemSort.ItemsortWeaponSingleHand;
        } // single hand use

        public bool IsWeaponTwoHand()
        {
            return IsWeaponTwoHand(Type);
        } // two hand use

        public static bool IsWeaponTwoHand(uint type)
        {
            return GetItemSort(type) == ItemSort.ItemsortWeaponDoubleHand;
        } // two hand use

        public bool IsWeaponProBased()
        {
            return IsWeaponProBased(Type);
        } // professional hand use

        public static bool IsWeaponProBased(uint type)
        {
            return GetItemSort(type) == ItemSort.ItemsortWeaponProfBased;
        } // professional hand use

        public bool IsWeapon()
        {
            return IsWeaponOneHand() || IsWeaponTwoHand() || IsWeaponProBased();
        }

        public static bool IsWeapon(uint type)
        {
            return IsWeaponOneHand(type) || IsWeaponTwoHand(type) || IsWeaponProBased(type);
        }

        public bool IsOther()
        {
            return GetItemSort() == ItemSort.ItemsortUsable;
        }

        public bool IsFinery()
        {
            return !IsArrowSort() && GetItemSort() == ItemSort.ItemsortFinery;
        }

        public bool IsShield()
        {
            return IsShield(Type);
        }

        public bool IsExpend()
        {
            return IsExpend(Type);
        }

        public int GetQuality()
        {
            return GetQuality(Type);
        }

        public static bool IsShield(uint nType)
        {
            return nType / 1000 == 900;
        }

        public static bool IsExpend(uint type)
        {
            return IsArrowSort(type)
                   || GetItemSort(type) == ItemSort.ItemsortUsable
                   || GetItemSort(type) == ItemSort.ItemsortUsable2
                   || GetItemSort(type) == ItemSort.ItemsortUsable3;
        }

        public static int GetQuality(uint type)
        {
            return (int) (type % 10);
        }

        public static bool IsBow(uint type)
        {
            return GetItemSubType(type) == 500;
        }

        public static bool IsArrowSort(uint type)
        {
            return GetItemtype(type) == 50000 && type != TYPE_JAR && !IsRing(type) && !IsBangle(type);
        }

        public static ItemSort GetItemSort(uint type)
        {
            return (ItemSort) (type % 10000000 / 100000);
        }

        public static int GetItemtype(uint type)
        {
            if (GetItemSort(type) == ItemSort.ItemsortWeaponSingleHand
                || GetItemSort(type) == ItemSort.ItemsortWeaponDoubleHand)
                return (int) (type % 100000 / 1000 * 1000);
            return (int) (type % 100000 / 10000 * 10000);
        }

        public static int GetItemSubType(uint type)
        {
            return (int) (type % 1000000 / 1000);
        }

        public int GetLevel()
        {
            return GetLevel(Type);
        }

        public static int GetLevel(uint type)
        {
            return (int) type % 1000 / 10;
        }

        #endregion

        #region Json

        public string ToJson()
        {
            return JsonConvert.SerializeObject(mDbItem);
        }

        #endregion

        #region Database

        public Task<bool> SaveAsync()
        {
            return ServerDbContext.SaveAsync(mDbItem);
        }

        public async Task<bool> DeleteAsync(ChangeOwnerType type = ChangeOwnerType.DeleteItem)
        {
            try
            {
                await ChangeOwnerAsync(0, type);
                mDbItem.OwnerId = 0;
                mDbItem.DeleteTime = UnixTimestamp.Now();
                return await SaveAsync();
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Problem when Delete item!");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        #endregion

        #region Socket

        public async Task SendJarAsync()
        {
            if (mUser == null)
                return;

            var msg = new MsgInteract
            {
                Action = MsgInteractType.Chop,
                SenderIdentity = PlayerIdentity,
                TargetIdentity = Identity,
                PosX = MaximumDurability,
                Command = (int) Data * 2
            };
            await mUser.SendAsync(msg);
        }

        #endregion

        #region Enums

        public enum ItemSort
        {
            ItemsortFinery = 1,
            ItemsortMount = 3,
            ItemsortWeaponSingleHand = 4,
            ItemsortWeaponDoubleHand = 5,
            ItemsortWeaponProfBased = 6,
            ItemsortUsable = 7,
            ItemsortWeaponShield = 9,
            ItemsortUsable2 = 10,
            ItemsortUsable3 = 12,
            ItemsortAccessory = 3,
            ItemsortTwohandAccessory = 35,
            ItemsortOnehandAccessory = 36,
            ItemsortBowAccessory = 37,
            ItemsortShieldAccessory = 38
        }

        public enum ItemEffect : ushort
        {
            None = 0,
            Poison = 0xC8,
            Life = 0xC9,
            Mana = 0xCA,
            Shield = 0xCB,
            Horse = 0x64
        }

        public enum SocketGem : byte
        {
            NormalPhoenixGem = 1,
            RefinedPhoenixGem = 2,
            SuperPhoenixGem = 3,

            NormalDragonGem = 11,
            RefinedDragonGem = 12,
            SuperDragonGem = 13,

            NormalFuryGem = 21,
            RefinedFuryGem = 22,
            SuperFuryGem = 23,

            NormalRainbowGem = 31,
            RefinedRainbowGem = 32,
            SuperRainbowGem = 33,

            NormalKylinGem = 41,
            RefinedKylinGem = 42,
            SuperKylinGem = 43,

            NormalVioletGem = 51,
            RefinedVioletGem = 52,
            SuperVioletGem = 53,

            NormalMoonGem = 61,
            RefinedMoonGem = 62,
            SuperMoonGem = 63,

            NormalTortoiseGem = 71,
            RefinedTortoiseGem = 72,
            SuperTortoiseGem = 73,

            NormalThunderGem = 101,
            RefinedThunderGem = 102,
            SuperThunderGem = 103,

            NormalGloryGem = 121,
            RefinedGloryGem = 122,
            SuperGloryGem = 123,

            NoSocket = 0,
            EmptySocket = 255
        }

        public enum ItemPosition : ushort
        {
            Inventory = 0,
            EquipmentBegin = 1,
            Headwear = 1,
            Necklace = 2,
            Armor = 3,
            RightHand = 4,
            LeftHand = 5,
            Ring = 6,
            Gourd = 7,
            Boots = 8,
            Garment = 9,
            AttackTalisman = 10,
            DefenceTalisman = 11,
            Steed = 12,
            RightHandAccessory = 15,
            LeftHandAccessory = 16,
            SteedArmor = 17,
            Crop = 18,
            EquipmentEnd = Crop,

            AltHead = 21,
            AltNecklace = 22,
            AltArmor = 23,
            AltWeaponR = 24,
            AltWeaponL = 25,
            AltRing = 26,
            AltBottle = 27,
            AltBoots = 28,
            AltGarment = 29,
            AltFan = 30,
            AltTower = 31,
            AltSteed = 32,

            UserLimit = 199,

            /// <summary>
            ///     Warehouse
            /// </summary>
            Storage = 201,

            /// <summary>
            ///     House WH
            /// </summary>
            Trunk = 202,

            /// <summary>
            ///     Sashes
            /// </summary>
            Chest = 203,

            Detained = 250,
            Floor = 254
        }

        public enum ItemColor : byte
        {
            None,
            Black = 2,
            Orange = 3,
            LightBlue = 4,
            Red = 5,
            Blue = 6,
            Yellow = 7,
            Purple = 8,
            White = 9
        }

        public enum ChangeOwnerType : byte
        {
            DropItem,
            PickupItem,
            TradeItem,
            CreateItem,
            DeleteItem,
            ItemUsage,
            DeleteDroppedItem,
            InvalidItemType,
            BoothSale,
            ClearInventory,
            DetainEquipment
        }

        #endregion

        #region Constants

        private readonly uint[] TALISMAN_SOCKET_QUALITY_ADDITION = {0, 0, 0, 0, 0, 0, 5, 10, 40, 1000};

        private readonly uint[] TALISMAN_SOCKET_PLUS_ADDITION =
        {
            0, 6, 30, 80, 240, 740, 2220, 6660, 20000, 60000, 62000,
            66000, 72000
        };

        private readonly uint[] TALISMAN_SOCKET_HOLE_ADDITION0 = {0, 160, 960};
        private readonly uint[] TALISMAN_SOCKET_HOLE_ADDITION1 = {0, 2000, 8000};

        /// <summary>
        ///     Item is owned by the holder. Cannot be traded or dropped.
        /// </summary>
        public const int ITEM_MONOPOLY_MASK = 1;

        /// <summary>
        ///     Item cannot be stored.
        /// </summary>
        public const int ITEM_STORAGE_MASK = 2;

        /// <summary>
        ///     Item cannot be dropped.
        /// </summary>
        public const int ITEM_DROP_HINT_MASK = 4;

        /// <summary>
        ///     Item cannot be sold.
        /// </summary>
        public const int ITEM_SELL_HINT_MASK = 8;

        public const int ITEM_NEVER_DROP_WHEN_DEAD_MASK = 16;
        public const int ITEM_SELL_DISABLE_MASK = 32;
        public const int ITEM_STATUS_NONE = 0;
        public const int ITEM_STATUS_NOT_IDENT = 1;
        public const int ITEM_STATUS_CANNOT_REPAIR = 2;
        public const int ITEM_STATUS_NEVER_DAMAGE = 4;
        public const int ITEM_STATUS_MAGIC_ADD = 8;

        //
        public const uint TYPE_DRAGONBALL = 1088000;
        public const uint TYPE_METEOR = 1088001;
        public const uint TYPE_METEORTEAR = 1088002;
        public const uint TYPE_TOUGHDRILL = 1200005;

        public const uint TYPE_STARDRILL = 1200006;

        //
        public const uint TYPE_DRAGONBALL_SCROLL = 720028; // Amount 10
        public const uint TYPE_METEOR_SCROLL = 720027;     // Amount 10

        public const uint TYPE_METEORTEAR_PACK = 723711; // Amount 5

        //
        public const uint TYPE_STONE1 = 730001;
        public const uint TYPE_STONE2 = 730002;
        public const uint TYPE_STONE3 = 730003;
        public const uint TYPE_STONE4 = 730004;
        public const uint TYPE_STONE5 = 730005;
        public const uint TYPE_STONE6 = 730006;
        public const uint TYPE_STONE7 = 730007;

        public const uint TYPE_STONE8 = 730008;

        //
        public const uint TYPE_MOUNT_ID = 300000;

        //
        public const uint TYPE_EXP_BALL = 723700;
        public const uint TYPE_EXP_POTION = 723017;

        public static readonly int[] BowmanArrows =
        {
            1050000, 1050001, 1050002, 1050020, 1050021, 1050022, 1050023, 1050030, 1050031, 1050032, 1050033, 1050040,
            1050041, 1050042, 1050043, 1050050, 1050051, 1050052
        };

        public const uint IRON_ORE = 1072010;
        public const uint COPPER_ORE = 1072020;
        public const uint EUXINITE_ORE = 1072031;
        public const uint SILVER_ORE = 1072040;
        public const uint GOLD_ORE = 1072050;

        public const uint OBLIVION_DEW = 711083;
        public const uint MEMORY_AGATE = 720828;

        public const uint PERMANENT_STONE = 723694;
        public const uint BIGPERMANENT_STONE = 723695;

        public const int LOTTERY_TICKET = 710212;
        public const uint SMALL_LOTTERY_TICKET = 711504;

        public const uint TYPE_JAR = 750000;

        private const int ITEMTYPE_PHYSIC = 10000;         //Ò©Æ·
        private const int ITEMTYPE_MEDICINE_HP = 10000;    //²¹ÑªÒ©
        private const int ITEMTYPE_MEDICINE_MP = 11000;    //²¹Ä§·¨Ò©
        private const int ITEMTYPE_POISON = 12000;         //¶¾Ò©
        private const int ITEMTYPE_SCROLL = 20000;         //¾íÖá
        private const int ITEMTYPE_SCROLL_SPECIAL = 20000; //ÌØÊâ¾íÖá£¬Èç£º»Ø³Ç¾í¡¢×£¸£¾íÖáµÈ
        private const int ITEMTYPE_SCROLL_MSKILL = 21000;  //Ä§·¨Ê¦¼¼ÄÜ¾íÖá
        private const int ITEMTYPE_SCROLL_SSKILL = 22000;  //Õ½Ê¿¼¼ÄÜ¾íÖá
        private const int ITEMTYPE_SCROLL_BSKILL = 23000;  //¹­¼ýÊÖ¼¼ÄÜ¾íÖá <== ¸ÄÎªÒìÄÜÕß

        #endregion
    }
}