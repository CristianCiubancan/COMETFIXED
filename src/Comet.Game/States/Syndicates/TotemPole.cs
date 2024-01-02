using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;

namespace Comet.Game.States.Syndicates
{
    public sealed class TotemPole
    {
        private DbTotemAdd m_Enhancement;

        public ConcurrentDictionary<uint, Totem> Totems = new();

        public TotemPole(Syndicate.TotemPoleType flag, DbTotemAdd enhance = null)
        {
            Type = flag;
            m_Enhancement = enhance;
        }

        public Syndicate.TotemPoleType Type { get; }
        public long Donation => Totems.Values.Sum(x => x.Points);
        public bool Locked { get; set; } = true;

        public int Enhancement
        {
            get => EnhancementExpiration.HasValue && EnhancementExpiration.Value > DateTime.Now
                       ? m_Enhancement.BattleAddition
                       : 0;
            set => m_Enhancement.BattleAddition = (byte) Math.Min(2, Math.Max(0, value));
        }

        public DateTime? EnhancementExpiration => m_Enhancement?.TimeLimit;

        public int BattlePower
        {
            get
            {
                var result = 0;
                long donation = Donation;
                if (donation >= 2000000)
                    result++;
                if (donation >= 4000000)
                    result++;
                if (donation >= 10000000)
                    result++;
                return Math.Min(3, result);
            }
        }

        public int SharedBattlePower => Enhancement + BattlePower;

        public int GetUserContribution(uint idUser)
        {
            return Totems.Values.Where(x => x.PlayerIdentity == idUser).Sum(x => x.Points);
        }

        public async Task<bool> SetEnhancementAsync(DbTotemAdd totem)
        {
            if (totem != null && await ServerDbContext.SaveAsync(totem))
            {
                m_Enhancement = totem;
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveEnhancementAsync()
        {
            if (m_Enhancement != null)
            {
                await ServerDbContext.DeleteAsync(m_Enhancement);
                m_Enhancement = null;
                return true;
            }

            return true;
        }
    }
}