using System.Collections.Concurrent;
using System.Threading;
using Comet.AI.States;

namespace Comet.AI.World.Maps
{
    /// <summary>
    ///     A block is a set of the map which will hold a collection with all entities in an area. This will help us
    ///     iterating over a limited number of roles when trying to process AI and movement. Instead of iterating a list with
    ///     thousand roles in the entire map, we'll just iterate the blocks around us.
    /// </summary>
    public class GameBlock
    {
        private int m_userCount;

        /// <summary>
        ///     Collection of roles currently inside of this block.
        /// </summary>
        public ConcurrentDictionary<uint, Role> RoleSet = new();

        public bool Add(Role role)
        {
            if (role is Character) Interlocked.Increment(ref m_userCount);
            return RoleSet.TryAdd(role.Identity, role);
        }

        public bool Remove(Role role)
        {
            bool remove = RoleSet.TryRemove(role.Identity, out _);
            if (role is Character && remove) Interlocked.Decrement(ref m_userCount);
            return remove;
        }

        public bool Remove(uint role)
        {
            bool remove = RoleSet.TryRemove(role, out Role target);
            if (target is Character && remove) Interlocked.Decrement(ref m_userCount);
            return remove;
        }

        /// <summary>
        ///     The width/height of a view block.
        /// </summary>
        public const int BLOCK_SIZE = 18;
    }
}