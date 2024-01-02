using System.Collections.Concurrent;
using System.Linq;
using Comet.Game.States;

namespace Comet.Game.World
{
    public sealed class IdentityGenerator
    {
        public static readonly IdentityGenerator MapItem = new(Role.MAPITEM_FIRST, Role.MAPITEM_LAST);
        public static readonly IdentityGenerator Monster = new(Role.MONSTERID_FIRST, Role.MONSTERID_LAST);
        public static readonly IdentityGenerator Pet = new(Role.CALLPETID_FIRST, Role.CALLPETID_LAST);
        public static readonly IdentityGenerator Furniture = new(Role.SCENE_NPC_MIN, Role.SCENE_NPC_MAX);
        public static readonly IdentityGenerator Traps = new(Role.MAGICTRAPID_FIRST, Role.MAGICTRAPID_LAST);

        private readonly ConcurrentQueue<long> mIdQueue = new();

        public IdentityGenerator(long min, long max)
        {
            for (long i = min; i <= max; i++) 
                mIdQueue.Enqueue(i);
        }

        public long GetNextIdentity => mIdQueue.TryDequeue(out long result) ? result : 0;

        public void ReturnIdentity(long id)
        {
            if (!mIdQueue.Contains(id))
                mIdQueue.Enqueue(id);
        }

        public int IdentitiesCount()
        {
            return mIdQueue.Count;
        }
    }
}