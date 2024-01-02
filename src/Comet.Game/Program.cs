using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Comet.Database;
using Comet.Game.Database;
using Comet.Game.Internal.AI;
using Comet.Game.Internal.Auth;
using Comet.Game.Packets;
using Comet.Network.Security;
using Comet.Shared;

namespace Comet.Game
{
    /// <summary>
    ///     The game server listens for authentication players with a valid access token from
    ///     the account server, and hosts the game world. The game world in this project has
    ///     been simplified into a single server executable. For an n-server distributed
    ///     systems implementation of a Conquer Online server, see Chimera.
    /// </summary>
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            // Copyright notice may not be commented out. If adding your own copyright or
            // claim of ownership, you may include a second copyright above the existing
            // copyright. Do not remove completely to comply with software license. The
            // project name and version may be removed or changed.
            Console.Title = "Comet, Game Server";
            Console.WriteLine();
            await Log.WriteLogAsync(LogLevel.Info, "  Comet: Game Server");
            await Log.WriteLogAsync(LogLevel.Info, $"  Copyright 2018-{DateTime.Now:yyyy} Gareth Jensen \"Spirited\"");
            await Log.WriteLogAsync(LogLevel.Info, "  All Rights Reserved");
            Console.WriteLine();

            // Read configuration file and command-line arguments
            var config = new ServerConfiguration(args);
            if (!config.Valid)
            {
                await Log.WriteLogAsync(LogLevel.Error, "Invalid server configuration file");
                return;
            }

            Kernel.Configuration = config;
            AccountClient.Configuration = config.RpcNetwork;

            // Initialize the database
            await Log.WriteLogAsync(LogLevel.Info, "Initializing server...");
            MsgConnect.StrictAuthentication = config.Authentication.StrictAuthPass;
            AbstractDbContext.Configuration = config.Database;
            if (!ServerDbContext.Ping())
            {
                await Log.WriteLogAsync(LogLevel.Error, "Invalid database configuration");
                return;
            }

            // Start background services (needed before init)
            var tasks = new List<Task>
            {
                Kernel.Services.Randomness.StartAsync(CancellationToken.None),
                NDDiffieHellman.ProbablePrimes.StartAsync(CancellationToken.None),
                Kernel.Services.Processor.StartAsync(CancellationToken.None)
            };
            Task.WaitAll(tasks.ToArray());

            if (!await Kernel.StartupAsync().ConfigureAwait(true))
            {
                await Log.WriteLogAsync(LogLevel.Error, "Could not load database related stuff");
                return;
            }

            // Start the RPC server listener
            await Log.WriteLogAsync("Launching server listeners...");

            var aiServer = new AiServer();
            _ = aiServer.StartAsync(config.AiNetwork.Port, config.AiNetwork.IPAddress, 10);

            // Start the game server listener
            var server = new Server(config);
            _ = server.StartAsync(config.GameNetwork.Port, config.GameNetwork.IPAddress)
                      .ConfigureAwait(false);

            // Output all clear and wait for user input
            await Log.WriteLogAsync("Listening for new connections");

            bool result = await CommandCenterAsync();

            await Kernel.CloseAsync().ConfigureAwait(true);

            if (!result)
                await Log.WriteLogAsync(LogLevel.Error, "Account server has exited without success.");
        }

        private static async Task<bool> CommandCenterAsync()
        {
            while (true)
            {
                string text = Console.ReadLine();

                if (string.IsNullOrEmpty(text))
                    continue;

                if (text == "exit")
                {
                    await Log.WriteLogAsync(LogLevel.Warning, "Server will shutdown...");
                    return true;
                }

                string[] full = text.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);

                if (full.Length <= 0)
                    continue;

                switch (full[0].ToLower())
                {
                    case "exit":
                    {
                        return true;
                    }
                }
            }
        }
    }
}