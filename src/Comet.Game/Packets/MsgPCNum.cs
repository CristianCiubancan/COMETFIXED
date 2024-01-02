using System.Threading.Tasks;
using Comet.Game.Internal.Auth;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets;

namespace Comet.Game.Packets
{
    public sealed class MsgPCNum : MsgBase<AccountServer>
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

        public override Task ProcessAsync(AccountServer client)
        {
            Character user = RoleManager.GetUserByAccount(AccountIdentity);
            if (user == null)
                return Task.CompletedTask;

            user.Client.MacAddress = MacAddress;
            return Task.CompletedTask;
        }
    }
}