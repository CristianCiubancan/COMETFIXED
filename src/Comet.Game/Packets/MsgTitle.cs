using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgTitle : MsgTitle<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user == null)
                return;

            switch (Action)
            {
                case TitleAction.Query:
                {
                    await user.SendTitlesAsync();
                    break;
                }

                case TitleAction.Select:
                {
                    if (Title != 0 && !user.HasTitle((Character.UserTitles) Title))
                        return;

                    user.UserTitle = Title;
                    await user.BroadcastRoomMsgAsync(this, true);
                    await user.SaveAsync();
                    break;
                }
            }
        }
    }
}