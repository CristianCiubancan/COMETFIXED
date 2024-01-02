using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgTraining : MsgTraining<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            switch (Action)
            {
                case Mode.RequestTime:
                {
                    TrainingTime = client.Character.CurrentTrainingMinutes;
                    await client.Character.SendAsync(this);
                    break;
                }

                case Mode.RequestEnter:
                {
                    if (!client.Character.IsBlessed || client.Character.CurrentTrainingMinutes == 0)
                    {
                        await client.Character.SendAsync(Language.StrCannotEnterTG);
                        return;
                    }

                    await client.Character.SendAsync(this);

                    if (client.Character.MapIdentity != 601)
                        await client.Character.EnterAutoExerciseAsync();
                    break;
                }

                case Mode.RequestRewardInfo:
                {
                    (int Level, ulong Experience) currData = client.Character.GetCurrentOnlineTGExp();

                    DbLevelExperience expInfo = ExperienceManager.GetLevelExperience((byte) currData.Level);
                    if (expInfo == null)
                        return;

                    var exp = (int) (currData.Experience / (double) expInfo.Exp * 10000000);
                    await client.Character.SendAsync(new MsgTrainingInfo
                    {
                        Experience = exp,
                        Level = currData.Level,
                        TimeRemaining = (ushort) (client.Character.CurrentTrainingTime -
                                                  Math.Min(client.Character.CurrentOfflineTrainingTime,
                                                           client.Character.CurrentTrainingTime)),
                        TimeUsed = Math.Min(client.Character.CurrentOfflineTrainingTime,
                                            client.Character.CurrentTrainingTime)
                    });
                    break;
                }

                case Mode.ClaimReward:
                {
                    await client.Character.LeaveAutoExerciseAsync();
                    break;
                }

                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"Unhandled MsgTraining::{Action}");
                    break;
            }
        }
    }
}