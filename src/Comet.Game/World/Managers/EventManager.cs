using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Shared;

namespace Comet.Game.World.Managers
{
    public static class EventManager
    {
        private static readonly ConcurrentDictionary<uint, DbAction> mActions = new();
        private static readonly ConcurrentDictionary<uint, DbTask> mTasks = new();

        private static readonly ConcurrentDictionary<GameEvent.EventType, GameEvent> mEvents = new();
        private static readonly List<QueuedAction> mQueuedActions = new();
        private static readonly TimeOut mRankingBroadcast = new(10);

        public static async Task<bool> InitializeAsync()
        {
            foreach (DbTask task in await TaskRepository.GetAsync()) mTasks.TryAdd(task.Id, task);

            foreach (DbAction action in await ActionRepository.GetAsync())
            {
                if (action.Type == 102)
                {
                    string[] response = action.Param.Split(' ');
                    if (response.Length < 2)
                        await Log.WriteLogAsync(LogLevel.Warning,
                                                $"Action [{action.Identity}] Type 102 doesn't set a task [param: {action.Param}]");
                    else if (response[1] != "0")
                        if (!uint.TryParse(response[1], out uint taskId) || !mTasks.ContainsKey(taskId))
                            await Log.WriteLogAsync(LogLevel.Warning, $"Task not found for action {action.Identity}");
                }

                mActions.TryAdd(action.Identity, action);
            }

            foreach (DbNpc dbNpc in await NpcRepository.GetAsync())
            {
                var npc = new Npc(dbNpc);

                if (!await npc.InitializeAsync())
                    await Log.WriteLogAsync(LogLevel.Warning, $"Could not load NPC {dbNpc.Id} {dbNpc.Name}");

                if (npc.Task0 != 0 && !mTasks.ContainsKey(npc.Task0))
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            $"Npc {npc.Identity} {npc.Name} no task found [taskid: {npc.Task0}]");
            }

            foreach (DbDynanpc dbDynaNpc in await DynaNpcRespository.GetAsync())
            {
                var npc = new DynamicNpc(dbDynaNpc);
                if (!await npc.InitializeAsync())
                    await Log.WriteLogAsync(LogLevel.Warning, $"Could not load NPC {dbDynaNpc.Id} {dbDynaNpc.Name}");

                if (npc.Task0 != 0 && !mTasks.ContainsKey(npc.Task0))
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            $"Npc {npc.Identity} {npc.Name} no task found [taskid: {npc.Task0}]");
            }

            await RegisterEventAsync(new ArenaQualifier());
            await RegisterEventAsync(new QuizShow());
            await RegisterEventAsync(new FamilyWar());

            mRankingBroadcast.Update();
            return true;
        }

        public static async Task ReloadActionTaskAllAsync()
        {
            mTasks.Clear();
            foreach (DbTask task in await TaskRepository.GetAsync()) mTasks.TryAdd(task.Id, task);
            await Log.WriteLogAsync(LogLevel.Debug, $"All Tasks has been reloaded. {mTasks.Count} in the server.");

            mActions.Clear();
            foreach (DbAction action in await ActionRepository.GetAsync())
            {
                if (action.Type == 102)
                {
                    string[] response = action.Param.Split(' ');
                    if (response.Length < 2)
                        await Log.WriteLogAsync(LogLevel.Warning,
                                                $"Action [{action.Identity}] Type 102 doesn't set a task [param: {action.Param}]");
                    else if (response[1] != "0")
                        if (!uint.TryParse(response[1], out uint taskId) || !mTasks.ContainsKey(taskId))
                            await Log.WriteLogAsync(LogLevel.Warning, $"Task not found for action {action.Identity}");
                }

                mActions.TryAdd(action.Identity, action);
            }

            await Log.WriteLogAsync(LogLevel.Debug, $"All Actions has been reloaded. {mActions.Count} in the server.");
        }

        public static DbAction GetAction(uint idAction)
        {
            return mActions.TryGetValue(idAction, out DbAction result) ? result : null;
        }

        public static DbTask GetTask(uint idTask)
        {
            return mTasks.TryGetValue(idTask, out DbTask result) ? result : null;
        }

        #region Events

        public static async Task<bool> RegisterEventAsync(GameEvent @event)
        {
            if (mEvents.ContainsKey(@event.Identity))
                return false;

            if (await @event.CreateAsync())
            {
                mEvents.TryAdd(@event.Identity, @event);
                return true;
            }

            return false;
        }

        public static void RemoveEvent(GameEvent.EventType type)
        {
            mEvents.TryRemove(type, out _);
        }

        public static T GetEvent<T>() where T : GameEvent
        {
            return mEvents.Values.FirstOrDefault(x => x.GetType() == typeof(T)) as T;
        }

        public static GameEvent GetEvent(GameEvent.EventType type)
        {
            return mEvents.TryGetValue(type, out GameEvent ev) ? ev : null;
        }

        public static GameEvent GetEvent(uint idMap)
        {
            return mEvents.Values.FirstOrDefault(x => x.Map?.Identity == idMap);
        }

        public static async Task DailyAsync()
        {
            foreach (GameEvent @event in mEvents.Values) await @event.DailyAsync();
        }

        public static bool QueueAction(QueuedAction action)
        {
            mQueuedActions.Add(action);
            return true;
        }

        #endregion

        public static async Task OnTimerAsync()
        {
            await PigeonManager.OnTimerAsync();

            bool ranking = mRankingBroadcast.ToNextTime();
            foreach (DynamicNpc dynaNpc in RoleManager.QueryRoleByType<DynamicNpc>())
            {
                if (dynaNpc.IsGoal())
                    continue;

                await dynaNpc.CheckFightTimeAsync();

                if (ranking && mEvents.Values.All(x => x.Map?.Identity != dynaNpc.MapIdentity))
                    await dynaNpc.BroadcastRankingAsync();
            }

            foreach (GameEvent @event in mEvents.Values)
                if (@event.ToNextTime())
                    await @event.OnTimerAsync();

            for (int i = mQueuedActions.Count - 1; i >= 0; i--)
            {
                QueuedAction action = mQueuedActions[i];
                Character user = RoleManager.GetUser(action.UserIdentity);
                if (action.CanBeExecuted && user != null)
                {
                    Item item = null;
                    if (user.InteractingItem != 0) item = user.UserPackage.FindByIdentity(user.InteractingItem);
                    Role role = null;
                    if (user.InteractingNpc != 0) role = RoleManager.GetRole(user.InteractingNpc);

                    await GameAction.ExecuteActionAsync(action.Action, user, role, item, "");
                    mQueuedActions.RemoveAt(i);
                }
            }
        }
    }
}