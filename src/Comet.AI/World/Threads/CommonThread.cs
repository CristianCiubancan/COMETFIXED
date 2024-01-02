using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Comet.AI.Packets;
using Comet.Shared;

namespace Comet.AI.World.Threads
{
    internal class CommonThread : TimerBase
    {
        private readonly TimeOutMS mAiPing = new();
        private readonly TimeOut mPingTimeout = new();

        public CommonThread()
            : base(1000, "Common Thread")
        {
            mAiPing.Startup(5000);
        }

        protected override async Task<bool> OnElapseAsync()
        {
            Console.Title = string.Format(APP_TITLE_S, DateTime.Now.ToString("s"),
                                          Kernel.NetworkMonitor.UpdateStatsAsync(1000));

            if ((Kernel.GameClient == null || Kernel.GameServer?.Socket.Connected != true) && mAiPing.ToNextTime())
            {
                await Log.WriteLogAsync(LogLevel.Info,
                                        $"Attempting connection with the account server on [{Client.Configuration.IPAddress}:{Client.Configuration.Port}]...");

                try
                {
                    Kernel.GameClient = new Client();
                    if (await Kernel.GameClient.ConnectToAsync(Client.Configuration.IPAddress,
                                                               Client.Configuration.Port))
                    {
                        mPingTimeout.Startup(15);
                        await Log.WriteLogAsync(LogLevel.Info, "Connected to the Game server!");
                    }
                }
                catch (SocketException ex)
                {
                    await Log.WriteLogAsync(LogLevel.Error, ex.Message);
                    Kernel.GameClient = null;
                }
                catch (Exception ex)
                {
                    await Log.WriteLogAsync(ex);
                    Kernel.GameClient = null;
                }
            }


            if (mPingTimeout.ToNextTime() && Kernel.GameClient != null && Kernel.GameServer?.Socket.Connected == true)
                await Kernel.GameServer.SendAsync(new MsgAiPing
                {
                    Timestamp = Environment.TickCount,
                    TimestampMs = Environment.TickCount64
                });

            return true;
        }

        private const string APP_TITLE_S = "Comet - Conquer Online NPC Server - {0} - {1}";
    }
}