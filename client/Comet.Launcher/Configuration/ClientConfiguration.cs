using Comet.Shared;
using Microsoft.Extensions.Configuration;

namespace Comet.Launcher.Configuration
{
    public sealed class ClientConfiguration
    {
        public ClientConfiguration()
        {
            new ConfigurationBuilder()
                .AddXmlFile(Path.Combine(FrmMain.DataPath, "AutoPatch.xml"))
                .Build()
                .Bind(this);
        }

        public List<string> Addresses { get; set; }
        public DateTime? TermsOfPrivacy { get; set; }
    }
}