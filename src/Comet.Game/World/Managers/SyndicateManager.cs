using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States.Syndicates;

namespace Comet.Game.World.Managers
{
    public static class SyndicateManager
    {
        private static readonly ConcurrentDictionary<ushort, Syndicate> m_dicSyndicates = new();

        public static async Task<bool> InitializeAsync()
        {
            List<DbSyndicate> dbSyndicates = await SyndicateRepository.GetAsync();
            foreach (DbSyndicate dbSyn in dbSyndicates)
            {
                var syn = new Syndicate();
                if (!await syn.CreateAsync(dbSyn))
                    continue;
                m_dicSyndicates.TryAdd(syn.Identity, syn);
            }

            foreach (Syndicate syndicate in m_dicSyndicates.Values) await syndicate.LoadRelationAsync();

            return true;
        }

        public static bool AddSyndicate(Syndicate syn)
        {
            return m_dicSyndicates.TryAdd(syn.Identity, syn);
        }

        public static Syndicate GetSyndicate(int idSyndicate)
        {
            return m_dicSyndicates.TryGetValue((ushort) idSyndicate, out Syndicate syn) ? syn : null;
        }

        public static Syndicate GetSyndicate(string name)
        {
            return m_dicSyndicates.Values.FirstOrDefault(
                x => x.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
        }

        /// <summary>
        ///     Find the syndicate a user is in.
        /// </summary>
        public static Syndicate FindByUser(uint idUser)
        {
            return m_dicSyndicates.Values.FirstOrDefault(x => x.QueryMember(idUser) != null);
        }

        public static Syndicate GetSyndicate(uint ownerIdentity)
        {
            return GetSyndicate((ushort) ownerIdentity);
        }
    }
}