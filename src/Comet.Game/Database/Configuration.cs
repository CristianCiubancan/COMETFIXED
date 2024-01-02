using Comet.Database;
using Microsoft.Extensions.Configuration;

namespace Comet.Game.Database
{
    /// <summary>
    ///     Defines the configuration file structure for the Game Server. App Configuration
    ///     files are copied to the build output directory on successful build, containing all
    ///     default configuration settings for the server, only if the file is newer than the
    ///     file bring replaced.
    /// </summary>
    public class ServerConfiguration
    {
        // Properties and fields
        public DatabaseConfiguration Database { get; set; }
        public GameNetworkConfiguration GameNetwork { get; set; }
        public RpcNetworkConfiguration RpcNetwork { get; set; }
        public AiNetworkConfiguration AiNetwork { get; set; }
        public AuthenticationConfiguration Authentication { get; set; }

        /// <summary>
        ///     Encapsulates network configuration for the game server listener.
        /// </summary>
        public class GameNetworkConfiguration
        {
            public string IPAddress { get; set; }
            public int Port { get; set; }
            public int MaxConn { get; set; }
            public string ServerName { get; set; }
            public uint ServerIdentity { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        /// <summary>
        ///     Encapsulates network configuration for the RPC server listener.
        /// </summary>
        public class RpcNetworkConfiguration
        {
            public string IPAddress { get; set; }
            public int Port { get; set; }
        }

        /// <summary>
        ///     Encapsulates network configuration for the RPC server listener.
        /// </summary>
        public class AiNetworkConfiguration
        {
            public string IPAddress { get; set; }
            public int Port { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
        }

        /// <summary>
        ///     Encapsulates authentication settings for client authentication between the
        ///     account server and game server.
        /// </summary>
        public class AuthenticationConfiguration
        {
            public bool StrictAuthPass { get; set; }
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="ServerConfiguration" /> with command-line
        ///     arguments from the user and a configuration file for the application. Builds the
        ///     configuration file and binds to this instance of the ServerConfiguration class.
        /// </summary>
        /// <param name="args">Command-line arguments from the user</param>
        public ServerConfiguration(string[] args)
        {
            new ConfigurationBuilder()
                .AddJsonFile("Comet.Game.config")
                .AddCommandLine(args)
                .Build()
                .Bind(this);
        }

        /// <summary>
        ///     Returns true if the server configuration is valid after reading.
        /// </summary>
        public bool Valid =>
            Database != null &&
            GameNetwork != null &&
            RpcNetwork != null &&
            Authentication != null;
    }
}