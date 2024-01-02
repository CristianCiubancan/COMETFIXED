using System;
using System.Threading.Tasks;
using Comet.Shared;

namespace Comet.Tools.GM.Threading
{
    internal sealed class UsualTaskThread : TimerBase
    {
        public delegate Task DisconnectCallback();

        public DisconnectCallback OnDisconnect;

        public UsualTaskThread(DisconnectCallback onDisconnect)
            : base(1000, "UsualTaskThread")
        {
            OnDisconnect = onDisconnect;
        }

        /// <inheritdoc />
        protected override async Task OnStartAsync()
        {
            _ = FrmMain.Instance.Invoke(async () => await FrmMain.Instance.DisplayLoginScreenAsync());
            await base.OnStartAsync();
        }

        /// <inheritdoc />
        protected override async Task<bool> OnElapseAsync()
        {
            
            return true;
        }
    }
}
