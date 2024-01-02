using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgMessageBoard : MsgMessageBoard<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Action)
            {
                case BoardAction.GetList:
                    List<MessageInfo> list = MessageBoard.GetMessages((TalkChannel) Channel, Index);
                    if (list.Count == 0)
                        return;

                    foreach (MessageInfo msg in list)
                    {
                        if (Messages.Count >= 8)
                            break;

                        Messages.Add(msg.Sender);
                        Messages.Add(msg.Message.Substring(0, Math.Min(44, msg.Message.Length)));
                        Messages.Add(msg.Time.ToString("yyyyMMddHHmmss"));
                    }

                    Action = BoardAction.List;
                    await user.SendAsync(this);
                    break;

                case BoardAction.GetWords:
                    string message = MessageBoard.GetMessage(Messages[0], (TalkChannel) Channel);
                    await user.SendAsync(new MsgTalk
                    {
                        Channel = (TalkChannel) Channel,
                        Color = Color.White,
                        Message = message,
                        SenderName = Messages[0],
                        RecipientName = user.Name,
                        Style = TalkStyle.Normal,
                        Suffix = ""
                    });
                    break;
            }
        }
    }
}