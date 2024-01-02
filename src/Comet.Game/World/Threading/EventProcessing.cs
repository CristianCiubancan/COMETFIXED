using System.Threading.Tasks;
using Comet.Game.World.Managers;
using Comet.Shared;

namespace Comet.Game.World.Threading
{
    public sealed class EventProcessing : TimerBase
    {
        public EventProcessing()
            : base(500, "EventsProcessing")
        {
        }

        protected override async Task<bool> OnElapseAsync()
        {
            await EventManager.OnTimerAsync();
            return true;
        }
    }
}