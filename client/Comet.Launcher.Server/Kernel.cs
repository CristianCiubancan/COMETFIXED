using System.Collections.Concurrent;
using System.Threading.Tasks;
using Comet.Launcher.Server.States;
using Comet.Network.Services;

namespace Comet.Launcher.Server
{
    public static class Kernel
    {
        public static class Services
        {
            public static RandomnessService Randomness = new();
        }

        public static ServerConfiguration Configuration { get; set; }

        public static ConcurrentDictionary<string, Client> Clients { get; } = new(5, 1000);

        /// <summary>
        ///     Returns the next random number from the generator.
        /// </summary>
        /// <param name="minValue">The least legal value for the Random number.</param>
        /// <param name="maxValue">One greater than the greatest legal return value.</param>
        public static Task<int> NextAsync(int minValue, int maxValue)
        {
            return Services.Randomness.NextAsync(minValue, maxValue);
        }
    }
}
