using System.Threading.Tasks;
using Comet.Game.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.States
{
    public class MessageBox
    {
        protected TimeOut m_expiration = new();
        protected Character m_owner;

        protected MessageBox(Character owner)
        {
            m_owner = owner;
        }

        public virtual string Message { get; protected set; }

        public virtual int TimeOut { get; protected set; }

        public bool HasExpired => TimeOut > 0 && m_expiration.IsTimeOut();

        public virtual Task OnAcceptAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnCancelAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task OnTimerAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task SendAsync()
        {
            m_expiration.Startup(TimeOut);
            return m_owner.SendAsync(new MsgTaskDialog
            {
                InteractionType = MsgTaskDialog<Client>.TaskInteraction.MessageBox,
                Text = Message,
                OptionIndex = 255,
                Data = (ushort) TimeOut
            });
        }
    }
}