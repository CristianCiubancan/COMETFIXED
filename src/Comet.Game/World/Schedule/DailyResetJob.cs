using System.Threading.Tasks;
using Comet.Game.World.Managers;
using Quartz;

namespace Comet.Game.World.Schedule
{
    [DisallowConcurrentExecution]
    public sealed class DailyResetJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            await RoleManager.OnDailyTriggerAsync();
        }
    }
}