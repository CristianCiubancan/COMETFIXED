using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.States;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgTaskDialog : MsgTaskDialog<Client>
    {
        public MsgTaskDialog()
        {
            Text = string.Empty;
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (InteractionType)
            {
                case TaskInteraction.MessageBox:
                {
                    if (user.MessageBox != null)
                    {
                        if (OptionIndex == 0)
                            await user.MessageBox.OnCancelAsync();
                        else
                            await user.MessageBox.OnAcceptAsync();

                        user.MessageBox = null;
                    }
                    else
                    {
                        uint idTask = user.GetTaskId(OptionIndex);

                        user.ClearTaskId();
                        await GameAction.ExecuteActionAsync(idTask, user,
                                                            RoleManager.GetRole(user.InteractingNpc),
                                                            user.UserPackage[user.InteractingItem], Text);
                    }

                    break;
                }

                case TaskInteraction.Answer:
                {
                    if (OptionIndex is 0 or byte.MaxValue) return;

                    Role targetRole = RoleManager.GetRole(user.InteractingNpc);
                    if (targetRole != null
                        && targetRole.MapIdentity != 5000
                        && targetRole.MapIdentity != user.MapIdentity)
                    {
                        user.CancelInteraction();
                        return;
                    }

                    if (targetRole != null
                        && targetRole.GetDistance(user) > Screen.VIEW_SIZE)
                    {
                        user.CancelInteraction();
                        return;
                    }

                    if (user.InteractingNpc == 0 && user.InteractingItem == 0)
                    {
                        user.CancelInteraction();
                        return;
                    }

                    uint idTask = user.GetTaskId(OptionIndex);
                    DbTask task = EventManager.GetTask(idTask);
                    if (task == null)
                    {
                        if (OptionIndex != 0)
                        {
                            user.CancelInteraction();

                            if (user.IsGm() && idTask != 0)
                                await user.SendAsync($"Could not find InteractionAsnwer for task {idTask}");
                        }

                        return;
                    }

                    user.ClearTaskId();
                    await GameAction.ExecuteActionAsync(await user.TestTaskAsync(task) ? task.IdNext : task.IdNextfail,
                                                        user,
                                                        targetRole, user.UserPackage[user.InteractingItem], Text);
                    break;
                }
                case TaskInteraction.TextInput:
                {
                    if (TaskIdentity == 31100)
                    {
                        if (user.SyndicateIdentity == 0 ||
                            user.SyndicateRank < SyndicateMember.SyndicateRank.DeputyLeader)
                            return;

                        await user.Syndicate.KickOutMemberAsync(user, Text);
                        await user.Syndicate.SendMembersAsync(0, user);
                    }

                    break;
                }
                default:
                    await Log.WriteLogAsync(LogLevel.Warning, $"MsgTaskDialog: {Type}, {InteractionType} unhandled");
                    break;
            }
        }
    }
}