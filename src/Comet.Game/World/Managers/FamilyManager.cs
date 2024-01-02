using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States.Families;

namespace Comet.Game.World.Managers
{
    public static class FamilyManager
    {
        private static readonly ConcurrentDictionary<uint, Family> m_dicFamilies = new();
        private static readonly ConcurrentDictionary<uint, DbFamilyBattleEffectShareLimit> m_familyBpLimit = new();

        public static async Task<bool> InitializeAsync()
        {
            List<DbFamily> dbFamilies = await FamilyRepository.GetAsync();
            foreach (DbFamily dbFamily in dbFamilies)
            {
                var family = await Family.CreateAsync(dbFamily);
                if (family != null)
                    m_dicFamilies.TryAdd(family.Identity, family);
            }

            foreach (Family family in m_dicFamilies.Values) family.LoadRelations();

            foreach (DbFamilyBattleEffectShareLimit limit in await FamilyBattleEffectShareLimitRepository.GetAsync())
                if (!m_familyBpLimit.ContainsKey(limit.Identity))
                    m_familyBpLimit.TryAdd(limit.Identity, limit);
            return true;
        }

        public static bool AddFamily(Family family)
        {
            return m_dicFamilies.TryAdd(family.Identity, family);
        }

        public static Family GetFamily(uint idFamily)
        {
            return m_dicFamilies.TryGetValue((ushort) idFamily, out Family family) ? family : null;
        }

        public static Family GetFamily(string name)
        {
            return m_dicFamilies.Values.FirstOrDefault(
                x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        public static Family GetOccupyOwner(uint idMap)
        {
            return m_dicFamilies.Values.FirstOrDefault(x => x.FamilyMap == idMap);
        }

        /// <summary>
        ///     Find the family a user is in.
        /// </summary>
        public static Family FindByUser(uint idUser)
        {
            return m_dicFamilies.Values.FirstOrDefault(x => x.GetMember(idUser) != null);
        }

        public static List<Family> QueryFamilies(Func<Family, bool> predicate)
        {
            return m_dicFamilies.Values.Where(predicate).ToList();
        }

        public static DbFamilyBattleEffectShareLimit GetSharedBattlePowerLimit(int level)
        {
            return m_familyBpLimit.Values.FirstOrDefault(x => x.Identity == level);
        }
    }
}