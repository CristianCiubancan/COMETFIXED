using System;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Packets;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.States
{
    public sealed class TradePartner
    {
        private readonly DbBusiness m_dbBusiness;

        public TradePartner(Character owner, DbBusiness business = null)
        {
            Owner = owner;
            if (business != null)
                m_dbBusiness = business;
        }

        public Character Owner { get; }

        public Character Target =>
            RoleManager.GetUser(m_dbBusiness.UserId == Owner.Identity
                                    ? m_dbBusiness.BusinessId
                                    : m_dbBusiness.UserId);

        public uint Identity => m_dbBusiness.UserId == Owner.Identity ? m_dbBusiness.BusinessId : m_dbBusiness.UserId;

        public string Name =>
            m_dbBusiness.UserId == Owner.Identity ? m_dbBusiness.Business?.Name : m_dbBusiness.User?.Name;

        public bool IsValid()
        {
            return m_dbBusiness.Date < DateTime.Now;
        }

        public Task SendAsync()
        {
            return Owner.SendAsync(new MsgTradeBuddy
            {
                Name = Name,
                Action = MsgTradeBuddy<Client>.TradeBuddyAction.AddPartner,
                IsOnline = Target != null,
                HoursLeft = (int) (!IsValid() ? (m_dbBusiness.Date - DateTime.Now).TotalMinutes : 0),
                Identity = Identity
            });
        }

        public Task SendInfoAsync()
        {
            Character target = Target;
            if (target == null)
                return Task.CompletedTask;

            return Owner.SendAsync(new MsgTradeBuddyInfo
            {
                Identity = Identity,
                Name = target.MateName,
                Level = target.Level,
                Lookface = target.Mesh,
                PkPoints = target.PkPoints,
                Profession = target.Profession,
                Syndicate = target.SyndicateIdentity,
                SyndicatePosition = (int) target.SyndicateRank
            });
        }

        public Task SendRemoveAsync()
        {
            return Owner.SendAsync(new MsgTradeBuddy
            {
                Action = MsgTradeBuddy<Client>.TradeBuddyAction.BreakPartnership,
                Identity = Identity,
                IsOnline = true,
                Name = ""
            });
        }

        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(m_dbBusiness);
        }
    }
}