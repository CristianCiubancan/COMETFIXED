using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.States;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgTaskStatus : MsgTaskStatus<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user == null) return;

            switch (Mode)
            {
                case TaskStatusMode.Update:
                {
                    await user.Statistic.InitializeAsync();
                    await user.TaskDetail.InitializeAsync();

                    foreach (TaskItemStruct item in Tasks)
                    {
                        DbTaskDetail task = user.TaskDetail.QueryTaskData((uint) item.Identity);
                        if (task == null)
                            item.Status = TaskItemStatus.Available;
                        else if (task.CompleteFlag != 0)
                            item.Status = TaskItemStatus.Done;
                        else if (task.NotifyFlag != 0)
                            item.Status = TaskItemStatus.AcceptedWithoutTrace;
                        else
                            item.Status = TaskItemStatus.Accepted;
                    }

                    await client.SendAsync(this);
                    break;
                }

                case TaskStatusMode.Add:
                {
                    foreach (TaskItemStruct item in Tasks)
                    {
                        DbTaskDetail task = user.TaskDetail.QueryTaskData((uint) item.Identity);
                        if (task == null)
                            await user.TaskDetail.CreateNewAsync((uint) item.Identity);
                    }

                    await client.SendAsync(this);
                    break;
                }

                default:
                {
                    await Log.WriteLogAsync(LogLevel.Socket, $"Unhandled MsgTaskStatus Mode {Mode}");
                    await Log.WriteLogAsync(PacketDump.Hex(Encode()));
                    break;
                }
            }
        }
    }
}