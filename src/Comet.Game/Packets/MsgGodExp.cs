using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgGodExp : MsgGodExp<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Action)
            {
                case MsgGodExpAction.Query:
                {
                    GodTimeExp = (int) user.GodTimeExp;
                    HuntExp = (int) user.OnlineTrainingExp;
                    await client.SendAsync(this);
                    break;
                }

                case MsgGodExpAction.ClaimHuntTraining:
                {
                    if (user.OnlineTrainingExp > 0)
                    {
                        await user.AwardExperienceAsync(user.CalculateExpBall((int) user.OnlineTrainingExp), true);
                        user.OnlineTrainingExp = 0;
                        await user.SaveAsync();

                        await client.SendAsync(new MsgGodExp
                        {
                            Action = MsgGodExpAction.Query,
                            GodTimeExp = (int) user.GodTimeExp,
                            HuntExp = (int) user.OnlineTrainingExp
                        });
                    }

                    break;
                }

                case MsgGodExpAction.ClaimOnlineTraining:
                {
                    if (user.GodTimeExp > 0)
                    {
                        await user.AwardExperienceAsync(user.CalculateExpBall((int) user.GodTimeExp), true);
                        user.GodTimeExp = 0;
                        await user.SaveAsync();

                        await client.SendAsync(new MsgGodExp
                        {
                            Action = MsgGodExpAction.Query,
                            GodTimeExp = (int) user.GodTimeExp,
                            HuntExp = (int) user.OnlineTrainingExp
                        });
                    }

                    break;
                }
            }
        }
    }
}