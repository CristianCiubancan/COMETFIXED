using System;
using System.Threading.Tasks;
using Comet.Game.Internal.Auth;
using Comet.Game.World.Managers;
using Comet.Shared;

namespace Comet.Game.World.Threading
{
    public sealed class SystemProcessor : TimerBase
    {
        public const string TITLE_FORMAT_S =
            @"[{0}] - Conquer Online Game Server [{5}] - {1} - Players: {3} (max:{4}) - {2} [{6}]";

        private readonly TimeOut m_analytics = new(300);
        private readonly TimeOut m_accountPing = new(5);
        private DateTime m_serverStartTime;

        public SystemProcessor()
            : base(1000, "System Thread")
        {
        }

        protected override Task OnStartAsync()
        {
            m_serverStartTime = DateTime.Now;
            m_analytics.Update();

            return base.OnStartAsync();
        }

        protected override async Task<bool> OnElapseAsync()
        {
            Console.Title = string.Format(TITLE_FORMAT_S, Kernel.GameConfiguration.ServerName,
                                          DateTime.Now.ToString("G"),
                                          Kernel.NetworkMonitor.UpdateStatsAsync(Interval), RoleManager.OnlinePlayers,
                                          RoleManager.MaxOnlinePlayers, Kernel.Version,
                                          Kernel.Services.Processor);

            if (m_analytics.ToNextTime()) await DoAnalyticsAsync();

            if ((Kernel.AccountClient == null || Kernel.AccountServer?.Socket.Connected != true) &&
                m_accountPing.ToNextTime())
            {
                await Log.WriteLogAsync(LogLevel.Info,
                                        $"Attempting connection with the account server on [{AccountClient.Configuration.IPAddress}:{AccountClient.Configuration.Port}]...");

                Kernel.AccountClient = new AccountClient();
                if (await Kernel.AccountClient.ConnectToAsync(AccountClient.Configuration.IPAddress,
                                                              AccountClient.Configuration.Port))
                    await Log.WriteLogAsync(LogLevel.Info, "Connected to the account server!");
            }

            return true;
        }

        public async Task DoAnalyticsAsync()
        {
            TimeSpan interval = DateTime.Now - m_serverStartTime;
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, "=".PadLeft(64, '='));
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, $"Server Start Time: {m_serverStartTime:G}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info,
                                    $"Total Online Time: {(int) interval.TotalDays} days, {interval.Hours} hours, {interval.Minutes} minutes, {interval.Seconds} seconds");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info,
                                    $"Online Players[{RoleManager.OnlinePlayers}], Max Online Players[{RoleManager.MaxOnlinePlayers}], Distinct Players[{RoleManager.OnlineUniquePlayers}], Role Count[{RoleManager.RolesCount}]");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info,
                                    $"Total Bytes Sent: {Kernel.NetworkMonitor.TotalBytesSent:N0}, Total Packets Sent: {Kernel.NetworkMonitor.TotalPacketsSent:N0}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info,
                                    $"Total Bytes Recv: {Kernel.NetworkMonitor.TotalBytesRecv:N0}, Total Packets Recv: {Kernel.NetworkMonitor.TotalPacketsRecv:N0}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, "Identities Remaining: ");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info,
                                    $"\tMonster: {IdentityGenerator.Monster.IdentitiesCount()}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info,
                                    $"\tFurniture: {IdentityGenerator.Furniture.IdentitiesCount()}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info,
                                    $"\tMapItem: {IdentityGenerator.MapItem.IdentitiesCount()}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info,
                                    $"\tTraps: {IdentityGenerator.Traps.IdentitiesCount()}");
            await Log.WriteLogAsync("GameAnalytics", LogLevel.Info, "=".PadLeft(64, '='));
        }
    }
}