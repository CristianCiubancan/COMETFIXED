#region References

using System.Threading.Tasks;
using Comet.Core;

#endregion

namespace Comet.Game.States.Syndicates
{
    public sealed class SyndicateRelationBox : MessageBox
    {
        private Character m_senderUser;
        private Character m_targetUser;

        private Syndicate m_sender;
        private Syndicate m_target;

        public SyndicateRelationBox(Character owner)
            : base(owner)
        {
        }

        public async Task<bool> CreateAsync(Character sender, Character target, RelationType type)
        {
            if (sender?.Syndicate == null || target?.Syndicate == null)
                return false;

            if (sender.SyndicateIdentity == target.SyndicateIdentity)
                return false;

            if (sender.Syndicate.Deleted || target.Syndicate.Deleted)
                return false;

            if (!sender.Syndicate.Leader.IsOnline || !target.Syndicate.Leader.IsOnline)
                return false;

            m_sender = sender.Syndicate;
            m_target = target.Syndicate;

            m_senderUser = sender;
            m_targetUser = target;

            Message = string.Format(Language.StrSyndicateAllianceRequest, sender.Name, sender.SyndicateName);
            await SendAsync();
            return true;
        }

        public override async Task OnAcceptAsync()
        {
            await m_sender.CreateAllianceAsync(m_senderUser, m_target);
        }

        public override async Task OnCancelAsync()
        {
            await m_sender.SendAsync(string.Format(Language.StrSyndicateAllianceDeny, m_target.Name));
        }

        public enum RelationType
        {
            Ally
        }
    }
}