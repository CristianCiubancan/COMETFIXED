using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.States;

namespace Comet.Game.World.Managers
{
    public static class ExperienceManager
    {
        private static readonly Dictionary<byte, DbLevelExperience> m_dicLevExp = new();
        private static readonly Dictionary<uint, DbPointAllot> m_dicPointAllot = new();
        private static Dictionary<uint, DbSuperman> m_superman = new();
        private static readonly List<DbRebirth> m_dicRebirths = new();
        private static readonly List<MagicTypeOp> m_magicOps = new();

        public static async Task<bool> InitializeAsync()
        {
            foreach (DbPointAllot auto in await PointAllotRepository.GetAsync())
                m_dicPointAllot.TryAdd(AllotIndex(auto.Profession, auto.Level), auto);

            foreach (DbLevelExperience lev in await LevelExperienceRepository.GetAsync())
                m_dicLevExp.TryAdd(lev.Level, lev);

            m_dicRebirths.AddRange(await RebirthRepository.GetAsync());

            foreach (DbMagictypeOp dbOp in await MagictypeOpRepository.GetAsync())
                m_magicOps.Add(new MagicTypeOp(dbOp));

            m_superman = (await SupermanRepository.GetAsync()).ToDictionary(superman => superman.UserIdentity);

            return true;
        }

        public static Task AddOrUpdateSupermanAsync(uint idUser, int amount)
        {
            if (!m_superman.TryGetValue(idUser, out DbSuperman superman))
                m_superman.Add(idUser, superman = new DbSuperman
                {
                    UserIdentity = idUser
                });

            superman.Amount = (uint) amount;
            return ServerDbContext.SaveAsync(superman);
        }

        public static int GetSupermanPoints(uint idUser)
        {
            return (int) (m_superman.TryGetValue(idUser, out DbSuperman value) ? value.Amount : 0);
        }

        public static int GetSupermanRank(uint idUser)
        {
            var result = 1;
            foreach (DbSuperman super in m_superman.Values.OrderByDescending(x => x.Amount))
            {
                if (super.UserIdentity == idUser)
                    return result;
                result++;
            }

            return result;
        }

        public static DbRebirth GetRebirth(int profNow, int profNext, int currMete)
        {
            profNow = profNow / 10 * 1000 + profNow % 10;
            profNext = profNext / 10 * 1000 + profNext % 10;
            return m_dicRebirths.FirstOrDefault(x => x.NeedProfession == profNow && x.NewProfession == profNext &&
                                                     x.Metempsychosis == currMete);
        }

        public static MagicTypeOp GetMagictypeOp(MagicTypeOp.MagictypeOperation op, int profNow, int profNext,
                                                 int metempsychosis)
        {
            return m_magicOps.FirstOrDefault(x => x.ProfessionAgo == profNow && x.ProfessionNow == profNext &&
                                                  x.RebirthTime == metempsychosis && x.Operation == op);
        }

        public static DbLevelExperience GetLevelExperience(byte level)
        {
            return m_dicLevExp.TryGetValue(level, out DbLevelExperience value) ? value : null;
        }

        public static int GetLevelLimit()
        {
            return m_dicLevExp.Count + 1;
        }

        public static DbPointAllot GetPointAllot(ushort profession, ushort level)
        {
            return m_dicPointAllot.TryGetValue(AllotIndex(profession, level), out DbPointAllot point) ? point : null;
        }

        private static uint AllotIndex(ushort prof, ushort level)
        {
            return (uint) ((prof << 16) + level);
        }
    }
}