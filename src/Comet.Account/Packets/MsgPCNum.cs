using System.Threading.Tasks;
using Comet.Account.Database;
using Comet.Account.States;
using Comet.Network.Packets;

namespace Comet.Account.Packets
{
    public sealed class MsgPCNum : MsgBase<Client>
    {
        public uint AccountIdentity;
        public string MacAddress;

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Type = (PacketType) reader.ReadUInt16();
            AccountIdentity = reader.ReadUInt32();
            MacAddress = reader.ReadString(12);
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgPCNum);
            writer.Write(AccountIdentity);
            writer.Write(MacAddress, 12);
            return writer.ToArray();
        }

        public override async Task ProcessAsync(Client client)
        {
            if (client.Account.AccountID != AccountIdentity)
                return;

            client.Account.MacAddress = MacAddress;
            await ServerDbContext.SaveAsync(client.Account);

            await client.Realm.GetServer<GameServer>().SendAsync(this);
        }
    }
}