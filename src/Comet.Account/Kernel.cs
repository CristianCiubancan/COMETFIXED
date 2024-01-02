using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Account.States;
using Comet.Database.Entities;
using Comet.Network.Services;

namespace Comet.Account
{
    /// <summary>
    ///     Kernel for the server, acting as a central core for pools of models and states
    ///     initialized by the server. Used in database repositories to load data into memory
    ///     from essential tables or tables which require heavy post-processing. Used in the
    ///     server packet process methods for tracking client and world states.
    /// </summary>
    public static class Kernel
    {
        // State caches
        public static Dictionary<string, DbRealm> Realms { get; set; }
        public static ConcurrentDictionary<uint, Client> Clients { get; } = new();
        public static ConcurrentDictionary<uint, Player> Players { get; } = new();

        /// <summary>
        ///     Returns the next random number from the generator.
        /// </summary>
        /// <param name="minValue">The least legal value for the Random number.</param>
        /// <param name="maxValue">One greater than the greatest legal return value.</param>
        public static Task<int> NextAsync(int minValue, int maxValue)
        {
            return Services.Randomness.NextAsync(minValue, maxValue);
        }

        // Background services
        public static class Services
        {
            public static RandomnessService Randomness = new();
        }
    }
}