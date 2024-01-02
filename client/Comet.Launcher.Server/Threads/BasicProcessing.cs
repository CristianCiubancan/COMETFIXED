using System;
using System.Threading.Tasks;
using Comet.Shared;

namespace Comet.Launcher.Server.Threads
{
    internal class BasicProcessing : TimerBase
    {
        private readonly TimeOut mPingTimeout = new(15);

        public BasicProcessing()
            : base(1000, "System Thread")
        {
        }

        protected override Task OnStartAsync()
        {
            mPingTimeout.Startup(15);
            return base.OnStartAsync();
        }

        protected override async Task<bool> OnElapseAsync()
        {
            Console.Title = string.Format(TITLE_S, 0);
            return true;
        }

        private const string TITLE_S = "Conquer Online Updater Server - Clients[{0}]";
    }
}