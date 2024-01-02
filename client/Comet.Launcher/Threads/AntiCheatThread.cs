#if !DEBUG
using Comet.Launcher.Managers;
#endif
using Comet.Shared;

namespace Comet.Launcher.Threads
{
    internal sealed class AntiCheatThread : TimerBase
    {
        public delegate void IllegalActionDelegate();

        /// <inheritdoc />
        public AntiCheatThread(IllegalActionDelegate onIllegalAction) 
            : base(1000, "Comet.Cheat.Thread")
        {
            OnIllegalAction = onIllegalAction;
        }

        public IllegalActionDelegate OnIllegalAction { get; }

        /// <inheritdoc />
        protected override Task OnStartAsync()
        {
#if !DEBUG
            //AntiCheatManager.LockDownLibraryLoadingAggressive();
            AntiDebuggerManager.AntiDebuggerAttach();
            if (AntiDebuggerManager.CloseHandleAntiDebug() || AntiDebuggerManager.RemoteDebuggerCheckAntiDebug())
            {
                OnIllegalAction?.Invoke();
                return Task.CompletedTask;
            }
            AntiDebuggerManager.HideThreadsFromDebugger();
#endif
            return base.OnStartAsync();
        }

        /// <inheritdoc />
        protected override Task<bool> OnElapseAsync()
        {
#if !DEBUG
            //AntiCheatManager.AntiUnHookerAggressive(OnIllegalAction);
            AntiDebuggerManager.OnTimer(OnIllegalAction);
#endif
            return Task.FromResult(true);
        }

        
    }
}
