using Microsoft.Extensions.Configuration;

namespace Comet.Launcher.Server
{
    /// <summary>
    ///     Defines the configuration file structure for the Account Server. App Configuration
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
                .AddJsonFile("Comet.Launcher.Server.Config.json")
                .AddCommandLine(args)
                .Build()
                .Bind(this);
        }

        // Properties and fields
        public NetworkConfiguration Network { get; set; }

        /// <summary>
        ///     Returns true if the server configuration is valid after reading.
        /// </summary>
        public bool Valid =>
            Network != null;

        /// <summary>
        ///     Encapsulates network configuration for the server listener.
        /// </summary>
        public class NetworkConfiguration
        {
            public string IpAddress { get; set; }
            public int Port { get; set; }
            public int MaxConn { get; set; }
        }
    }
}
