using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.World.Managers;
using Comet.Shared;

namespace Comet.Game.States
{
    public sealed class CaptchaBox : MessageBox
    {
        private readonly TimeOut m_Expiration = new();
        private readonly Character m_Owner;

        public CaptchaBox(Character owner)
            : base(owner)
        {
            m_Owner = owner;
        }

        public long Value1 { get; private set; }
        public long Value2 { get; private set; }
        public long Result { get; private set; }

        public override Task OnAcceptAsync()
        {
            if (Value1 + Value2 != Result)
                return RoleManager.KickOutAsync(m_Owner.Identity, "Wrong captcha reply");
            return Task.CompletedTask;
        }

        public override Task OnCancelAsync()
        {
            if (Value1 + Value2 == Result)
                return RoleManager.KickOutAsync(m_Owner.Identity, "Wrong captcha reply");
            return Task.CompletedTask;
        }

        public override Task OnTimerAsync()
        {
            if (m_Expiration.IsActive() && m_Expiration.IsTimeOut())
                return RoleManager.KickOutAsync(m_Owner.Identity, "No captcha reply");
            return Task.CompletedTask;
        }

        public async Task GenerateAsync()
        {
            Value1 = await Kernel.NextAsync(int.MaxValue) % 10;
            Value2 = await Kernel.NextAsync(int.MaxValue) % 10;
            if (await Kernel.ChanceCalcAsync(50, 100))
                Result = Value1 + Value2;
            else
                Result = Value1 + Value2 + await Kernel.NextAsync(int.MaxValue) % 10;

            Message = string.Format(Language.StrBotCaptchaMessage, Value1, Value2, Result);
            TimeOut = 60;

            await SendAsync();
            m_Expiration.Startup(60);
        }
    }
}