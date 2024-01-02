using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgQuiz : MsgQuiz<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            var quiz = EventManager.GetEvent<QuizShow>();
            if (quiz == null)
                return;

            switch (Action)
            {
                case QuizAction.Reply:
                {
                    if (quiz.IsCanceled(user.Identity))
                        return;

                    await quiz.OnReplyAsync(user, Param1, Param2);
                    return;
                }

                case QuizAction.Quit:
                {
                    if (quiz.IsCanceled(user.Identity))
                        return;

                    quiz.Cancel(user.Identity);
                    return;
                }

                default:
                {
                    await client.SendAsync(this);
                    if (client.Character.IsPm())
                        await client.SendAsync(new MsgTalk(client.Identity, TalkChannel.Service,
                                                           $"Missing packet {Type}, Action {Action}, Length {Length}"));

                    await Log.WriteLogAsync(LogLevel.Warning,
                                            "Missing packet {0}, Action {1}, Length {2}\n{3}",
                                            Type, Action, Length, PacketDump.Hex(Encode()));
                    return;
                }
            }
        }
    }
}