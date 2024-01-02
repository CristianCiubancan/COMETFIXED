using System.Threading.Tasks;
using Comet.Game.States.Items;
using Comet.Game.World.Maps;
using Comet.Shared;

namespace Comet.Game.States.Events
{
    public abstract class GameEvent
    {
        protected enum EventStage
        {
            Idle,
            Running,
            Ending
        }

        public enum EventType
        {
            None,
            TimedGuildWar,
            GuildPk,
            GuildContest,
            LineSkillPk,
            ArenaQualifier,
            QuizShow,
            Limit
        }

        public const int RANK_REFRESH_RATE_MS = 10000;

        private readonly TimeOutMS m_eventCheck;

        protected GameEvent(string name, int timeCheck = 1000)
        {
            Name = name;
            m_eventCheck = new TimeOutMS(timeCheck);
        }

        public virtual EventType Identity { get; } = EventType.None;

        public string Name { get; }

        protected EventStage Stage { get; set; } = EventStage.Idle;

        public virtual GameMap Map { get; protected set; }

        public virtual bool IsInTime { get; } = false;
        public virtual bool IsActive { get; } = false;
        public virtual bool IsEnded { get; } = false;

        public virtual bool IsAttackEnable(Role sender)
        {
            return true;
        }

        public bool ToNextTime()
        {
            return m_eventCheck.ToNextTime();
        }

        public virtual bool IsAllowedToJoin(Role sender)
        {
            return true;
        }

        public virtual Task<bool> CreateAsync()
        {
            return Task.FromResult(true);
        }

        public virtual Task OnEnterAsync(Character sender)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnExitAsync(Character sender)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnMoveAsync(Character sender)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnAttackAsync(Character sender)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnBeAttackAsync(Role attacker, Role target, int damage = 0, Magic magic = null)
        {
            return Task.CompletedTask;
        }

        public virtual Task<int> GetDamageLimitAsync(Role attacker, Role target, int power)
        {
            return Task.FromResult(power);
        }

        public virtual Task OnHitAsync(Role attacker, Role target, Magic magic = null) // magic null is auto attack
        {
            return Task.CompletedTask;
        }

        public virtual Task OnKillAsync(Role attacker, Role target, Magic magic = null)
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> OnReviveAsync(Character sender, bool selfRevive)
        {
            return Task.FromResult(false);
        }

        public virtual Task OnTimerAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task DailyAsync()
        {
            return Task.CompletedTask;
        }

        public virtual Task<bool> OnActionCommandAsync(string param, Character user, Role role, Item item, string input)
        {
            return Task.FromResult(true);
        }

        public virtual Task<(uint id, ushort x, ushort y)> GetRevivePositionAsync(Character sender)
        {
            return Task.FromResult((sender.RecordMapIdentity, sender.RecordMapX, sender.RecordMapY));
        }
    }
}