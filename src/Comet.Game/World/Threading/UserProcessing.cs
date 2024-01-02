using System.Threading.Tasks;
using Comet.Game.World.Managers;
using Comet.Shared;

namespace Comet.Game.World.Threading
{
    public class UserProcessing : TimerBase
    {
        public UserProcessing()
            : base(60, "User Thread")
        {
        }

        protected override async Task<bool> OnElapseAsync()
        {
            await RoleManager.OnTimerAsync();
            return true;
        }
    }
}