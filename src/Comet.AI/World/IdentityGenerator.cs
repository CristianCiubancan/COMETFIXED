using System.Collections.Concurrent;
using System.Linq;
using Comet.AI.States;

namespace Comet.AI.World
{
    public sealed class IdentityGenerator
    {
        private readonly ConcurrentQueue<long> m_cqidQueue = new();
        private readonly long m_idMax = uint.MaxValue;
        private readonly long m_idMin;
        private long m_idNext;

        public IdentityGenerator(long min, long max)
        {
            m_idNext = m_idMin = min;
            m_idMax = max;

            for (long i = m_idMin; i <= m_idMax; i++) m_cqidQueue.Enqueue(i);

            m_idNext = m_idMax + 1;
        }

        public long GetNextIdentity
        {
            get
            {
                if (m_cqidQueue.TryDequeue(out long result))
                    return result;
                return 0;
            }
        }

        public void ReturnIdentity(long id)
        {
            if (!m_cqidQueue.Contains(id))
                m_cqidQueue.Enqueue(id);
        }

        public int IdentitiesCount()
        {
            return m_cqidQueue.Count;
        }

        public static IdentityGenerator MapItem = new(Role.MAPITEM_FIRST, Role.MAPITEM_LAST);
        public static IdentityGenerator Monster = new(Role.MONSTERID_FIRST, Role.MONSTERID_LAST);
        public static IdentityGenerator Furniture = new(Role.SCENE_NPC_MIN, Role.SCENE_NPC_MAX);
        public static IdentityGenerator Traps = new(Role.MAGICTRAPID_FIRST, Role.MAGICTRAPID_LAST);
    }
}