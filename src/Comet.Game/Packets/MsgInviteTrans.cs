using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgInviteTrans : MsgInviteTrans<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Mode)
            {
                case Action.Accept:
                {
                    if (!(user.MessageBox is EventInvitationBox box))
                        return;

                    if (box.HasExpired)
                    {
                        user.MessageBox = null;
                        return;
                    }

                    await user.MessageBox.OnAcceptAsync();
                    await user.SendAsync(new MsgInviteTrans
                    {
                        Mode = Action.AcceptMessage,
                        Message = box.AcceptMsgId
                    });

                    user.MessageBox = null;
                    break;
                }
            }
        }
    }
}