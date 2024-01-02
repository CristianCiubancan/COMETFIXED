#define BETA

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Comet.Account.Database;
using Comet.Account.Database.Repositories;
using Comet.Account.Packets;
using Comet.Account.Threading;
using Comet.Database;
using Comet.Database.Entities;
using Comet.Shared;

namespace Comet.Account
{
    /// <summary>
    ///     The account server accepts clients and authenticates players from the client's
    ///     login screen. If the player enters valid account credentials, then the server
    ///     will send login details to the game server and disconnect the client. The client
    ///     will reconnect to the game server with an access token from the account server.
    /// </summary>
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            Log.DefaultFileName = "LoginServer";

            // Copyright notice may not be commented out. If adding your own copyright or
            // claim of ownership, you may include a second copyright above the existing
            // copyright. Do not remove completely to comply with software license. The
            // project name and version may be removed or changed.
            Console.Title = @"Comet, Account Server";
            Console.WriteLine();
            await Log.WriteLogAsync(LogLevel.Info, "  Comet: Account Server");
            await Log.WriteLogAsync(LogLevel.Info, $"  Copyright 2018-{DateTime.Now:yyyy} Gareth Jensen \"Spirited\"");
            await Log.WriteLogAsync(LogLevel.Info, "  All Rights Reserved");
            Console.WriteLine();

            // Read configuration file and command-line arguments
            var config = new ServerConfiguration(args);
            if (!config.Valid)
            {
                await Log.WriteLogAsync(LogLevel.Info, "Invalid server configuration file");
                return;
            }

            MsgAccServerExchange.RealmDataKey = new byte[config.DatabaseKey.Length / 2];
            for (var index = 0; index < MsgAccServerExchange.RealmDataKey.Length; index++)
            {
                string byteValue = config.DatabaseKey.Substring(index * 2, 2);
                MsgAccServerExchange.RealmDataKey[index] = Convert.ToByte(byteValue, 16);
            }

            // Initialize the database
            await Log.WriteLogAsync(LogLevel.Info, "Initializing server...");
            AbstractDbContext.Configuration = config.Database;
            if (!ServerDbContext.Ping())
            {
                await Log.WriteLogAsync(LogLevel.Info, "Invalid database configuration");
                return;
            }

            // Recover caches from the database
            var tasks = new List<Task>();
            tasks.Add(RealmsRepository.LoadAsync());
            Task.WaitAll(tasks.ToArray());

            // Start background services
            tasks = new List<Task>();
            tasks.Add(Kernel.Services.Randomness.StartAsync(CancellationToken.None));
            Task.WaitAll(tasks.ToArray());

            await Log.WriteLogAsync(LogLevel.Info, "Launching realm server listener...");
            var realmServer = new IntraServer(config);
            _ = realmServer.StartAsync(config.RealmNetwork.Port);

            // Start the server listener
            await Log.WriteLogAsync(LogLevel.Info, "Listening for new connections");
            var server = new Server(config);
            _ = server.StartAsync(config.Network.Port, config.Network.IPAddress)
                      .ConfigureAwait(false);

            BasicProcessing thread = new();
            await thread.StartAsync();

            // Output all clear and wait for user input
            await Log.WriteLogAsync(LogLevel.Info, "Listening for new connections");
            Console.WriteLine();
            bool result = await CommandCenterAsync();

            await thread.CloseAsync();
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
#if DEBUG || BETA
                    case "account":
                    {
                        if (full.Length < 3)
                        {
                            Console.WriteLine(@"account [username] [password]");
                            continue;
                        }

                        string username = full[1];
                        string password = full[2];

                        if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                        {
                            Console.WriteLine(@"Username and Password cannot be Null or Empty.");
                            continue;
                        }

                        string salt = ConquerAccount.GenerateSalt();
                        DbAccount dbAccount = new ()
                        {
                            StatusID = DbAccount.AccountStatus.None,
                            Username = username,
                            Password = ConquerAccount.HashPassword(password, salt),
                            Salt = salt,
                            AuthorityID = 255, // don't use in production
                            IPAddress = "127.0.0.1",
                            Registered = DateTime.Now,
                            MacAddress = "CMD_ACC",
                            ParentId = 1
                        };

                        if (!await ServerDbContext.SaveAsync(dbAccount))
                        {
                            Console.WriteLine(@"Could not save account to database.");
                            continue;
                        }

                        Console.WriteLine($@"User created with name {username} and password {password}");
                        continue;
                    }
#endif

                    case "exit":
                    {
                        return true;
                    }
                }
            }
        }
    }
}