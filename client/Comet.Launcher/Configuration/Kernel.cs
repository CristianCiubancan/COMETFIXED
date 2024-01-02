using System.Diagnostics;
using System.Runtime.InteropServices;
using Comet.Launcher.Managers;
using Comet.Shared;

namespace Comet.Launcher.Configuration
{
    public static class Kernel
    {
        private static string AutoPatchConfigFile => Path.Combine(Environment.CurrentDirectory, "AutoPatch", "AutoPatch.xml");
        private static string AutoPatchUserConfigFile => Path.Combine(Environment.CurrentDirectory, "AutoPatch", "AutoPatchUserConfig.xml");

        public static ClientConfiguration ClientConfiguration { get; private set; }
        public static UserConfiguration UserConfiguration { get; private set; }
        
        public static bool Initialize()
        {
            try
            {
                ClientConfiguration = new ClientConfiguration();
            }
            catch
            {
                MessageBox.Show(LocaleManager.GetString("StrClientSettingsNotLoaded"),
                                LocaleManager.GetString("StrSettings"));
                return false;
            }

            try
            {
                UserConfiguration = new UserConfiguration();
            }
            catch
            {
                if (File.Exists(AutoPatchUserConfigFile))
                    File.Delete(AutoPatchUserConfigFile);

                XmlParser xml = new XmlParser(AutoPatchUserConfigFile);
                xml.AddNewNode(40, null, "FramesPerSecond", "Config");
                xml.AddNewNode(1024, null, "Width", "Config", "Resolution");
                xml.AddNewNode(768, null, "Height", "Config", "Resolution");
                xml.Save();

                UserConfiguration = new UserConfiguration();
            }

            return true;
        }

        public static void OpenUrl(string url)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                url = url.Replace("&", "^&");
                Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
        }
    }
}
