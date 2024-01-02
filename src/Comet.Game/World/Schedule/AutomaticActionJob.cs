using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Shared;
using Quartz;

namespace Comet.Game.World.Schedule
{
    [DisallowConcurrentExecution]
    public sealed class AutomaticActionJob : IJob
    {
        private const int _ACTION_SYSTEM_EVENT = 2030000;
        private const int _ACTION_SYSTEM_EVENT_LIMIT = 9999;

        private readonly ConcurrentDictionary<uint, DbAction> m_dicActions;

        public AutomaticActionJob()
        {
            m_dicActions = new ConcurrentDictionary<uint, DbAction>(1, _ACTION_SYSTEM_EVENT_LIMIT);

            for (var a = 0; a < _ACTION_SYSTEM_EVENT_LIMIT; a++)
            {
                DbAction action = EventManager.GetAction((uint) (_ACTION_SYSTEM_EVENT + a));
                if (action != null)
                    m_dicActions.TryAdd(action.Identity, action);
            }
        }

        public async Task Execute(IJobExecutionContext context)
        {
            foreach (DbAction action in m_dicActions.Values)
                try
                {
                    await GameAction.ExecuteActionAsync(action.Identity, null, null, null, "");
                }
                catch (Exception ex)
                {
                    await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                }
        }
    }
}