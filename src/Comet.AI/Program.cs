using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Comet.AI.Database;
using Comet.Database;
using Comet.Shared;

namespace Comet.AI
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
            Log.DefaultFileName = "AiServer";

            Console.Title = @"Comet, Ai Server";
            Console.WriteLine();
            await Log.WriteLogAsync(LogLevel.Info, "  Comet: AI Server");
            await Log.WriteLogAsync(LogLevel.Info,
                                    $"  Copyright 2022-{DateTime.Now:yyyy} Felipe Vieira Vendramini \"Konichu\"");
            await Log.WriteLogAsync(LogLevel.Info, "  All Rights Reserved");
            Console.WriteLine();

            await Log.WriteLogAsync(LogLevel.Info, "Initializing server...");
            // Read configuration file and command-line arguments
            ServerConfiguration config = Kernel.Configuration = new ServerConfiguration(args);
            if (!config.Valid)
            {
                await Log.WriteLogAsync(LogLevel.Info, "Invalid server configuration file");
                return;
            }

            Client.Configuration = config.GameNetwork;
            AbstractDbContext.Configuration = config.Database;
            if (!ServerDbContext.Ping())
            {
                await Log.WriteLogAsync(LogLevel.Info, "Invalid database configuration");
                return;
            }

            // Start background services
            var tasks = new List<Task>();
            tasks.Add(Kernel.Services.Randomness.StartAsync(CancellationToken.None));
            tasks.Add(Kernel.Services.Processor.StartAsync(CancellationToken.None));
            Task.WaitAll(tasks.ToArray());

            if (!await Kernel.InitializeAsync())
            {
                await Log.WriteLogAsync(LogLevel.Error, "Could not initialize AI Server!!");
                return;
            }

            Console.WriteLine();
            bool result = await CommandCenterAsync();

            await Kernel.DestroyAsync().ConfigureAwait(true);

            if (!result)
                await Log.WriteLogAsync(LogLevel.Error, "AI server has exited without success.");
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