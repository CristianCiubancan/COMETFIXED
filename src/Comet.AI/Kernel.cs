using System;
using System.Threading.Tasks;
using Comet.AI.Database;
using Comet.AI.States;
using Comet.AI.World;
using Comet.AI.World.Managers;
using Comet.AI.World.Threads;
using Comet.Network.Packets;
using Comet.Network.Services;
using Comet.Shared;

namespace Comet.AI
{
    public static class Kernel
    {
        public static ServerConfiguration Configuration { get; set; }

        public static NetworkMonitor NetworkMonitor { get; } = new();

        public static Client GameClient { get; set; }
        public static Server GameServer { get; set; }

        private static CommonThread CommonThread { get; } = new();
        private static AiThread AiThread { get; } = new();
        private static GeneratorThread GeneratorThread { get; } = new();

        public static async Task<bool> InitializeAsync()
        {
            try
            {
                await MapManager.LoadMapsAsync().ConfigureAwait(true);

                await RoleManager.InitializeAsync().ConfigureAwait(true);
                await GeneratorManager.InitializeAsync().ConfigureAwait(true);

                await CommonThread.StartAsync();
                await AiThread.StartAsync();
                await GeneratorThread.StartAsync();

                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(ex);
                return false;
            }
        }

        public static async Task DestroyAsync()
        {
            await AiThread.CloseAsync();
            await GeneratorThread.CloseAsync();
            await CommonThread.CloseAsync();
        }

        /// <summary>
        ///     Sends a message to the game server.
        /// </summary>
        /// <param name="msg">The packet to be send to the game server.</param>
        /// <returns>The amount of bytes sent to the server.</returns>
        public static Task<int> SendAsync(IPacket msg)
        {
            if (GameServer != null && GameServer.Socket.Connected)
                return GameServer.SendAsync(msg);
            return Task.FromResult(-1);
        }

        /// <summary>
        ///     Returns the next random number from the generator.
        /// </summary>
        /// <param name="maxValue">One greater than the greatest legal return value.</param>
        public static Task<int> NextAsync(int maxValue)
        {
            return NextAsync(0, maxValue);
        }

        public static Task<double> NextRateAsync(double range)
        {
            return Services.Randomness.NextRateAsync(range);
        }

        /// <summary>Writes random numbers from the generator to a buffer.</summary>
        /// <param name="buffer">Buffer to write bytes to.</param>
        public static Task NextBytesAsync(byte[] buffer)
        {
            return Services.Randomness.NextBytesAsync(buffer);
        }

        /// <summary>
        ///     Returns the next random number from the generator.
        /// </summary>
        /// <param name="minValue">The least legal value for the Random number.</param>
        /// <param name="maxValue">One greater than the greatest legal return value.</param>
        public static Task<int> NextAsync(int minValue, int maxValue)
        {
            return Services.Randomness.NextAsync(minValue, maxValue);
        }

        public static async Task<bool> ChanceCalcAsync(int chance, int outOf)
        {
            return await NextAsync(outOf) < chance;
        }

        public static class Services
        {
            public static readonly RandomnessService Randomness = new();
#if DEBUG
            public static readonly ServerProcessor Processor = new(2);
#else
            public readonly static ServerProcessor Processor = new ServerProcessor(Environment.ProcessorCount/2);
#endif
        }
    }
}