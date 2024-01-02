using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Comet.Launcher.Server.States.Patches;
using Comet.Launcher.Server.Threads;
using Comet.Shared;

namespace Comet.Launcher.Server
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            Console.CancelKeyPress += ConsoleOnCancelKeyPress;

            Log.DefaultFileName = "LauncherServer";

            // Copyright notice may not be commented out. If adding your own copyright or
            // claim of ownership, you may include a second copyright above the existing
            // copyright. Do not remove completely to comply with software license. The
            // project name and version may be removed or changed.
            Console.Title = @"Comet, Auto Updater Server";
            Console.WriteLine();
            await Log.WriteLogAsync(LogLevel.Info, "  Comet: Auto Updater Server");
            await Log.WriteLogAsync(LogLevel.Info,
                                    $"  Copyright 2022-{DateTime.Now:yyyy} Felipe Vieira Vendramini \"Konichu\"");
            await Log.WriteLogAsync(LogLevel.Info, "  Some Rights Reserved");
            Console.WriteLine();

            // Read configuration file and command-line arguments
            var config = new ServerConfiguration(args);
            if (!config.Valid)
            {
                await Log.WriteLogAsync(LogLevel.Info, "Invalid server configuration file");
                return;
            }

            Kernel.Configuration = config;

            await UpdateManager.LoadConfigAsync();

            var tasks = new List<Task>();
            tasks.Add(Kernel.Services.Randomness.StartAsync(CancellationToken.None));
            Task.WaitAll(tasks.ToArray());

            await Log.WriteLogAsync(LogLevel.Info, "Launching updater server listener...");
            var realmServer = new Server(config);
            _ = realmServer.StartAsync(config.Network.Port);

            BasicProcessing thread = new();
            await thread.StartAsync();

            // Output all clear and wait for user input
            await Log.WriteLogAsync(LogLevel.Info, "Listening for new connections");
            Console.WriteLine();
            bool result = await CommandCenterAsync();
            await thread.CloseAsync();
            if (!result)
                await Log.WriteLogAsync(LogLevel.Error, "Updater server has exited without success.");
        }

        private static void ConsoleOnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
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
                    case "add":
                    {
                        Console.WriteLine("This command will add a new patch to the queue.");
                        Console.WriteLine(
                            "***REMINDER*** Updates above 10000 will be handled as primary updates that will force the updater client to restart.");
                        Console.WriteLine();
                        Console.Write("Please, type the number of the patch you want to add: ");

                        if (!int.TryParse(Console.ReadLine(), out int num))
                        {
                            Console.WriteLine("Invalid patch number.");
                            continue;
                        }

                        Console.Write("Please, type the file extension [7z, exe]: ");
                        string ext = Console.ReadLine();

                        if (UpdateManager.ContainsUpdate(num))
                        {
                            Console.Write(
                                "The current patch is already on the queue. Do you want to override it? [Y/N]");
                            if (Console.ReadLine()?.Equals("Y") != true)
                                continue;
                            UpdateManager.RemovePatch(num);
                        }

                        Console.Write("Please, type the file SHA-256 hash: ");
                        string hash = Console.ReadLine();

                            var update = new UpdateStruct
                        {
                            From = num,
                            Extension = ext,
                            To = num,
                            Hash = hash
                        };
                        if (UpdateManager.AppendPatch(update))
                        {
                            await Log.WriteLogAsync(LogLevel.Info, $"Patch {num} has been added.");
                        }
                        else
                        {
                            await Log.WriteLogAsync(LogLevel.Error, $"Could not add patch {num}.");
                        }

                        break;
                    }

                    case "add-bundle":
                    {
                        Console.WriteLine("This command will add a new patch to the queue.");
                        Console.WriteLine(
                            "***REMINDER*** Updates above 10000 will be handled as primary updates that will force the updater client to restart.");
                        Console.WriteLine();
                        Console.Write("Please, type the number of the patch you want to add: ");

                        if (!int.TryParse(Console.ReadLine(), out int num))
                        {
                            Console.WriteLine("Invalid patch number.");
                            continue;
                        }

                        Console.Write("Please, type the final patch it'll update: ");
                        if (!int.TryParse(Console.ReadLine(), out int target))
                        {
                            Console.WriteLine("Invalid patch number.");
                            continue;
                        }

                        Console.Write("Please, type the file extension [7z, exe]: ");
                        string ext = Console.ReadLine();

                        if (UpdateManager.ContainsUpdate(num))
                        {
                            Console.Write("The current patch is already on the queue. Do you want to override it? [Y/N]");
                            if (Console.ReadLine()?.Equals("Y") != true)
                                continue;
                            UpdateManager.RemovePatch(num);
                        }

                        Console.Write("Please, type the file SHA-256 hash: ");
                        string hash = Console.ReadLine();

                        var update = new UpdateStruct
                        {
                            From = num,
                            Extension = ext,
                            To = target,
                            Hash = hash
                        };
                        if (UpdateManager.AppendPatch(update))
                        {
                            await Log.WriteLogAsync(LogLevel.Info, $"Patch '{update.FileName}' has been added.");
                        }
                        else
                        {
                            await Log.WriteLogAsync(LogLevel.Error, $"Could not add patch {num}.");
                        }

                        break;
                    }

                    case "remove":
                    {
                        Console.Write("Write the patch FROM identity: ");
                        if (!int.TryParse(Console.ReadLine(), out int from))
                        {
                            Console.WriteLine("Invalid patch ID. Must be a number.");
                            continue;
                        }

                        if (!UpdateManager.ContainsUpdate(from))
                        {
                            Console.WriteLine("Patch does not exist.");
                            continue;
                        }

                        if (UpdateManager.RemovePatch(from))
                        {
                            Console.WriteLine($"Patch {from} removed.");
                        }
                        else
                        {
                            Console.WriteLine($"Could not remove patch {from}.");
                        }

                        Console.WriteLine();

                        break;
                    }

                    case "list":
                    {
                        Console.WriteLine();
                        UpdateManager.ListUpdates();
                        Console.WriteLine();
                        break;
                    }

                    case "test":
                    {
                        Console.WriteLine("From what patch are you trying to update: ");
                        if (!int.TryParse(Console.ReadLine(), out int from))
                        {
                            Console.WriteLine("Value is not a Integer.");
                            continue;
                        }

                        Console.WriteLine();

                        Console.WriteLine("Updates list");
                        foreach (UpdateStruct patch in UpdateManager.GetUpdateSequence(from))
                        {
                            Console.WriteLine($"\tFrom({patch.From:0000}), To({patch.To:0000}) -> {patch.FullFileName} [{patch.Hash}]");
                        }

                        Console.WriteLine();

                        break;
                    }

                    case "cls":
                    {
                        Console.Clear();
                        break;
                    }
                }
            }
        }
    }
}