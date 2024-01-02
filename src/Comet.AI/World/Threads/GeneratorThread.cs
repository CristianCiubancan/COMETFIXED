using System.Threading.Tasks;
using Comet.AI.World.Managers;
using Comet.Shared;

namespace Comet.AI.World.Threads
{
    public class GeneratorThread : TimerBase
    {
        public GeneratorThread()
            : base(1000, "Generator Thread")
        {
        }

        protected override async Task<bool> OnElapseAsync()
        {
            await GeneratorManager.OnTimerAsync();
            return true;
        }
    }
}