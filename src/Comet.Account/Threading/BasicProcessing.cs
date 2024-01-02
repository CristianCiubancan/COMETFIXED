using System;
using System.Linq;
using System.Threading.Tasks;
using Comet.Account.Packets;
using Comet.Account.States;
using Comet.Database.Entities;
using Comet.Shared;

namespace Comet.Account.Threading
{
    public sealed class BasicProcessing : TimerBase
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
            Console.Title = string.Format(TITLE_S, Kernel.Realms.Values.Count(x => x.Server != null),
                                          Kernel.Players.Count, DateTime.Now.ToString("G"));

            if (mPingTimeout.ToNextTime())
                foreach (DbRealm realm in Kernel.Realms.Values)
                    if (realm.Server != null)
                        await realm.GetServer<GameServer>().SendAsync(new MsgAccServerPing());
            return true;
        }

        private const string TITLE_S = "Conquer Online Account Server - Servers[{0}], Players[{1}] - {2}";
    }
}