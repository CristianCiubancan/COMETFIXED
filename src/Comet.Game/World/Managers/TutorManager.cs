using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;

namespace Comet.Game.World.Managers
{
    public static class TutorManager
    {
        private static readonly List<DbTutorType> m_tutorType = new();
        private static readonly List<DbTutorBattleLimitType> m_tutorBattleLimitTypes = new();

        public static async Task<bool> InitializeAsync()
        {
            m_tutorType.AddRange(await TutorTypeRepository.GetAsync());
            m_tutorBattleLimitTypes.AddRange(await TutorBattleLimitTypeRepository.GetAsync());
            return true;
        }

        public static DbTutorBattleLimitType GetTutorBattleLimitType(int delta)
        {
            return m_tutorBattleLimitTypes.Aggregate((x, y) => Math.Abs(x.Id - delta) < Math.Abs(y.Id - delta) ? x : y);
        }

        public static DbTutorType GetTutorType(int level)
        {
            return m_tutorType.FirstOrDefault(x => level >= x.UserMinLevel && level <= x.UserMaxLevel);
        }
    }
}