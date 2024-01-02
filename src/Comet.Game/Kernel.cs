using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Caching;
using System.Threading;
using System.Threading.Tasks;
using Comet.Game.Database;
using Comet.Game.Internal.AI;
using Comet.Game.Internal.Auth;
using Comet.Game.World;
using Comet.Game.World.Managers;
using Comet.Game.World.Schedule;
using Comet.Game.World.Threading;
using Comet.Network.Packets;
using Comet.Network.Services;
using Comet.Shared;

namespace Comet.Game
{
    /// <summary>
    ///     Kernel for the server, acting as a central core for pools of models and states
    ///     initialized by the server. Used in database repositories to load data into memory
    ///     from essential tables or tables which require heavy post-processing. Used in the
    ///     server packet process methods for tracking client and world states.
    /// </summary>
    public static class Kernel
    {
        public static readonly string Version;

        static Kernel()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "Error";
        }

        // State caches
        public static readonly MemoryCache Logins = MemoryCache.Default;
        public static readonly List<uint> Registration = new();

        #region Login

        /// <summary>
        ///     The account server client object.
        /// </summary>
        public static AccountServer AccountServer;

        /// <summary>
        ///     The account server client socket.
        /// </summary>
        public static AccountClient AccountClient;

        #endregion

        #region AI

        public static AiClient AiServer { get; set; }

        public static async Task BroadcastWorldMsgAsync(IPacket msg)
        {
            if (AiServer != null)
            {
                await AiServer.SendAsync(msg);
            }
        }

        #endregion

        public static ServerConfiguration.GameNetworkConfiguration GameConfiguration => Configuration.GameNetwork;
        public static ServerConfiguration Configuration { get; set; }

        public static readonly NetworkMonitor NetworkMonitor = new();

        public static readonly SystemProcessor SystemThread = new();
        public static readonly UserProcessing UserThread = new();
        public static readonly EventProcessing EventThread = new();
        public static readonly RoleProcessing RoleThread = new();

        private static SchedulerFactory SchedulerFactory { get; set; }

        // Background services
        public static class Services
        {
            public static readonly RandomnessService Randomness = new();
#if DEBUG
            public static readonly ServerProcessor Processor = new();
#else
            public readonly static ServerProcessor Processor = new();
#endif
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

        public static async Task<bool> StartupAsync()
        {
            try
            {
                await MapManager.LoadMapsAsync().ConfigureAwait(true);

                await MagicManager.InitializeAsync().ConfigureAwait(true);
                await SyndicateManager.InitializeAsync().ConfigureAwait(true);
                await FamilyManager.InitializeAsync().ConfigureAwait(true);
                await EventManager.InitializeAsync().ConfigureAwait(true);
                await RoleManager.InitializeAsync().ConfigureAwait(true);
                await ItemManager.InitializeAsync().ConfigureAwait(true);
                await PigeonManager.InitializeAsync().ConfigureAwait(true);
                await PeerageManager.InitializeAsync().ConfigureAwait(true);
                await TutorManager.InitializeAsync().ConfigureAwait(true);
                await FlowerManager.InitializeAsync().ConfigureAwait(true);
                await MineManager.InitializeAsync().ConfigureAwait(true);
                await ExperienceManager.InitializeAsync().ConfigureAwait(true);
                await LotteryManager.InitializeAsync().ConfigureAwait(true);

                await SystemThread.StartAsync();
                await UserThread.StartAsync();
                await EventThread.StartAsync();
                await RoleThread.StartAsync();

                SchedulerFactory = new SchedulerFactory();
                await SchedulerFactory.StartAsync();
                await SchedulerFactory.ScheduleAsync<AutomaticActionJob>("0 * * * * ?");
                await SchedulerFactory.ScheduleAsync<DailyResetJob>("0 0 0 * * ?");
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public static async Task<bool> CloseAsync()
        {
            await SchedulerFactory.StopAsync();

            await UserThread.CloseAsync();
            await EventThread.CloseAsync();
            await RoleThread.CloseAsync();

            await Services.Processor.StopAsync(new CancellationToken(true)).ConfigureAwait(true);
            await RoleManager.KickOutAllAsync("Server is now closing", true).ConfigureAwait(true);

            for (var i = 0; i < 5; i++)
            {
                await Log.WriteLogAsync(LogLevel.Info, $"Server will shutdown in {5 - i} seconds...");
                await Task.Delay(1000);
            }

            await SystemThread.CloseAsync();
            return true;
        }

        /// <summary>
        ///     Calculates the chance of success based in a rate.
        /// </summary>
        /// <param name="chance">Rate in percent.</param>
        /// <returns>True if the rate is successful.</returns>
        public static async Task<bool> ChanceCalcAsync(double chance)
        {
            const int divisor = 10_000_000;
            const int maxValue = 100 * divisor;
            try
            {
                return await NextAsync(0, maxValue) <= chance * divisor;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Chance Calc error!");
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public static bool IsValidName(string szName)
        {
            foreach (char c in szName)
            {
                if (c < ' ')
                    return false;
                switch (c)
                {
                    case ' ':
                    case ';':
                    case ',':
                    case '/':
                    case '\\':
                    case '=':
                    case '%':
                    case '@':
                    case '\'':
                    case '"':
                    case '[':
                    case ']':
                    case '?':
                    case '{':
                    case '}':
                        return false;
                }
            }

            string lower = szName.ToLower();
            return InvalidNameChar.All(part => !lower.Contains(part));
        }

        private static readonly string[] InvalidNameChar =
        {
            "{", "}", "[", "]", "(", ")", "\"", "[gm]", "[pm]", "'", "´", "`", "admin", "helpdesk", " ",
            "bitch", "puta", "whore", "ass", "fuck", "cunt", "fdp", "porra", "poha", "caralho", "caraio"
        };
    }
}