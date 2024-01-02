using System.Collections.Generic;
using System.Linq;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgUserAttrib<T> : MsgBase<T>
    {
        private readonly List<UserAttribute> Attributes = new();

        public MsgUserAttrib()
        {
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, ulong value)
        {
            Type = PacketType.MsgUserAttrib;

            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint) type, value));
        }

        public MsgUserAttrib(uint idRole, ClientUpdateType type, uint value0, uint value1)
        {
            Type = PacketType.MsgUserAttrib;

            Identity = idRole;
            Amount++;
            Attributes.Add(new UserAttribute((uint) type, value0, value1));
        }

        public uint Identity { get; set; }

        public int Amount { get; set; }

        public List<UserAttribute> GetAttributes()
        {
            return Attributes.ToList();
        }

        public void Append(ClientUpdateType type, ulong data)
        {
            Amount++;
            Attributes.Add(new UserAttribute((uint) type, data));
        }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Identity = reader.ReadUInt32();
            Amount = reader.ReadInt32();
            for (var i = 0; i < Amount; i++)
            {
                uint type = reader.ReadUInt32();
                ulong data = reader.ReadUInt64();
                Attributes.Add(new UserAttribute(type, data));
            }
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) Type);
            //writer.Write(Environment.TickCount);
            writer.Write(Identity);
            Amount = Attributes.Count;
            writer.Write(Amount);
            for (var i = 0; i < Amount; i++)
            {
                writer.Write(Attributes[i].Type);
                writer.Write(Attributes[i].Data);
            }

            return writer.ToArray();
        }

        public readonly struct UserAttribute
        {
            public UserAttribute(uint type, ulong data)
            {
                Type = type;
                Data = data;
            }

            public UserAttribute(uint type, uint left, uint right)
            {
                Type = type;
                Data = ((ulong) left << 32) | right;
            }

            public readonly uint Type;
            public readonly ulong Data;
        }
    }

    public enum ClientUpdateType
    {
        Hitpoints = 0,
        MaxHitpoints = 1,
        Mana = 2,
        MaxMana = 3,
        Money = 4,
        Experience = 5,
        PkPoints = 6,
        Class = 7,
        Stamina = 8,
        Atributes = 10,
        Mesh,
        Level,
        Spirit,
        Vitality,
        Strength,
        Agility,
        HeavensBlessing,
        DoubleExpTimer,
        CursedTimer = 20,
        Reborn = 22,
        StatusFlag = 25,
        HairStyle = 26,
        XpCircle = 27,
        LuckyTimeTimer = 28,
        ConquerPoints = 29,
        OnlineTraining = 31,
        ExtraBattlePower = 36,
        Merchant = 38,
        VipLevel = 39,
        QuizPoints = 40,
        EnlightenPoints = 41,
        FamilySharedBattlePower = 42,
        TotemPoleBattlePower = 44,
        BoundConquerPoints = 45,
        AzureShield = 49,
        SoulShackleTimer = 54,

        Vigor = 10000
    }
}