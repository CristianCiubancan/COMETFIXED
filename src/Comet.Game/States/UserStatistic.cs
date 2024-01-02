using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;

namespace Comet.Game.States
{
    public sealed class UserStatistic
    {
        private readonly ConcurrentDictionary<ulong, DbStatistic> m_dicStc = new();

        private readonly Character m_pOwner;

        public UserStatistic(Character user)
        {
            m_pOwner = user;
        }

        public async Task<bool> InitializeAsync()
        {
            try
            {
                List<DbStatistic> list = await StatisticRepository.GetAsync(m_pOwner.Identity);
                if (list != null)
                    foreach (DbStatistic st in list)
                        m_dicStc.TryAdd(GetKey(st.EventType, st.DataType), st);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> AddOrUpdateAsync(uint idEvent, uint idType, uint data, bool bUpdate)
        {
            ulong key = GetKey(idEvent, idType);
            if (m_dicStc.TryGetValue(key, out DbStatistic stc))
            {
                stc.Data = data;
                if (bUpdate)
                    stc.Timestamp = DateTime.Now;
            }
            else
            {
                stc = new DbStatistic
                {
                    Data = data,
                    DataType = idType,
                    EventType = idEvent,
                    PlayerIdentity = m_pOwner.Identity,
                    Timestamp = DateTime.Now
                };
                m_dicStc.TryAdd(key, stc);
            }

            return await ServerDbContext.SaveAsync(stc);
        }

        public async Task<bool> SetTimestampAsync(uint idEvent, uint idType, DateTime? data)
        {
            DbStatistic stc = GetStc(idEvent, idType);
            if (stc == null)
            {
                await AddOrUpdateAsync(idEvent, idType, 0, true);
                stc = GetStc(idEvent, idType);

                if (stc == null)
                    return false;
            }

            stc.Timestamp = data;
            return await ServerDbContext.SaveAsync(stc);
        }

        public uint GetValue(uint idEvent, uint idType = 0)
        {
            return m_dicStc.FirstOrDefault(x => x.Key == GetKey(idEvent, idType)).Value?.Data ?? 0u;
        }

        public DbStatistic GetStc(uint idEvent, uint idType = 0)
        {
            return m_dicStc.FirstOrDefault(x => x.Key == GetKey(idEvent, idType)).Value;
        }

        public bool HasEvent(uint idEvent, uint idType)
        {
            return m_dicStc.ContainsKey(GetKey(idEvent, idType));
        }

        private ulong GetKey(uint idEvent, uint idType)
        {
            return idEvent + ((ulong) idType << 32);
        }

        public async Task<bool> SaveAllAsync()
        {
            return await ServerDbContext.SaveAsync(m_dicStc.Values.ToList());
        }
    }
}