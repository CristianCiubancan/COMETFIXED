using System.Threading.Tasks;
using Comet.Game.World.Managers;
using Comet.Shared;

namespace Comet.Game.World.Threading
{
    public class RoleProcessing : TimerBase
    {
        public RoleProcessing()
            : base(200, "Ai Thread")
        {
        }

        protected override async Task<bool> OnElapseAsync()
        {
            //await RoleManager.OnRoleTimerAsync();
            await MapManager.OnTimerAsync();
            return true;
        }
    }
}