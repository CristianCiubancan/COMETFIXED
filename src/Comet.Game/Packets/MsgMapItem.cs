using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgMapItem : MsgMapItem<Client>
    {
        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (!user.IsAlive)
            {
                await user.SendAsync(Language.StrDead);
                return;
            }

            switch (Mode)
            {
                case DropType.PickupItem:
                    if (await user.SynPositionAsync(MapX, MapY, 0))
                    {
                        await user.PickMapItemAsync(Identity);
                        await user.BroadcastRoomMsgAsync(this, true);
                    }

                    break;
                default:
                    await client.SendAsync(new MsgTalk(client.Identity, TalkChannel.Service,
                                                       $"Missing packet {Type}, Action {Mode}, Length {Length}"));
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            "Missing packet {0}, Action {1}, Length {2}\n{3}",
                                            Type, Mode, Length, PacketDump.Hex(Encode()));
                    break;
            }
        }
    }
}