using Comet.Database;
using Microsoft.Extensions.Configuration;

namespace Comet.AI.Database
{
    /// <summary>
    ///     Defines the configuration file structure for the Game Server. App Configuration
    ///     files are copied to the build output directory on successful build, containing all
    ///     default configuration settings for the server, only if the file is newer than the
    ///     file bring replaced.
    /// </summary>
    public class ServerConfiguration
    {
        /// <summary>
        ///     Instantiates a new instance of <see cref="ServerConfiguration" /> with command-line
        ///     arguments from the user and a configuration file for the application. Builds the
        ///     configuration file and binds to this instance of the ServerConfiguration class.
        /// </summary>
        /// <param name="args">Command-line arguments from the user</param>
        public ServerConfiguration(string[] args)
        {
            new ConfigurationBuilder()
                .AddJsonFile("Comet.Ai.config")
                .AddCommandLine(args)
                .Build()
                .Bind(this);
        }

        // Properties and fields
        public DatabaseConfiguration Database { get; set; }
        public GameNetworkConfiguration GameNetwork { get; set; }

        /// <summary>
        ///     Returns true if the server configuration is valid after reading.
        /// </summary>
        public bool Valid =>
            Database != null &&
            GameNetwork != null;

        /// <summary>
        ///     Encapsulates network configuration for the game server listener.
        /// </summary>
        public class GameNetworkConfiguration
        {
            public string IPAddress { get; set; }
            public int Port { get; set; }
            public string ServerName { get; set; }
            public int ServerIdentity { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }
    }
}