using Comet.Game.States;
using Comet.Game.States.Npcs;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgNpcInfoEx : MsgNpcInfoEx<Client>
    {
        public MsgNpcInfoEx(DynamicNpc npc)
        {
            Identity = npc.Identity;
            MaxLife = npc.MaxLife;
            Life = npc.Life;
            PosX = npc.MapX;
            PosY = npc.MapY;
            Lookface = (ushort) npc.Mesh;
            NpcType = npc.Type;
            Sort = (ushort) npc.Sort;
            Name = npc.IsSynFlag() ? npc.Name : "";
        }

        public MsgNpcInfoEx(BoothNpc npc)
        {
            Identity = npc.Identity;
            MaxLife = npc.MaxLife;
            Life = npc.Life;
            PosX = npc.MapX;
            PosY = npc.MapY;
            Lookface = (ushort) npc.Mesh;
            NpcType = npc.Type;
            Sort = (ushort) npc.Sort;
            Name = npc.Name;
        }
    }
}