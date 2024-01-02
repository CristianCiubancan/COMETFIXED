using System.Drawing;
using System.Threading.Tasks;
using Comet.Game.Internal.AI;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets.Ai
{
    /// <remarks>Packet Type 1004</remarks>
    /// <summary>
    ///     Message defining a chat message from one player to the other, or from the system
    ///     to a player. Used for all chat systems in the game, including messages outside of
    ///     the game world state, such as during character creation or to tell the client to
    ///     continue logging in after connect.
    /// </summary>
    public sealed class MsgTalk : MsgTalk<AiClient>
    {
        public MsgTalk()
        {
        }

        public override async Task ProcessAsync(AiClient client)
        {
            Role sender = RoleManager.GetRole(CharacterID);
            if (sender == null)
                return;

            switch (Channel)
            {
                case TalkChannel.Talk:
                {
                    if (!sender.IsAlive)
                        return;

                    await sender.BroadcastRoomMsgAsync(this, false);
                    break;
                }
            }
        }
    }
}