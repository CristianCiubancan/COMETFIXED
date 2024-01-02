using System.Globalization;
using System.Reflection;
using System.Resources;

namespace Comet.Launcher.Managers
{
    public sealed class LocaleManager
    {
        public static List<Languages> AvailableLanguages = new()
        {
            new Languages {LanguageFullName = "Português Brasileiro", LanguageCultureName = "pt-BR"}
        };

        public static string CurrentlySelectedLanguage;

        public static bool IsLanguageAvailable(string lang)
        {
            return AvailableLanguages.FirstOrDefault(a => a.LanguageCultureName.Equals(lang)) != null;
        }

        public static string GetDefaultLanguage()
        {
            return AvailableLanguages[0].LanguageCultureName;
        }

        public static void SetLanguage(string lang)
        {
            try
            {
                if (!IsLanguageAvailable(lang)) lang = GetDefaultLanguage();
                var cultureInfo = new CultureInfo(lang);
                Thread.CurrentThread.CurrentUICulture = cultureInfo;
                Thread.CurrentThread.CurrentCulture = CultureInfo.CreateSpecificCulture(cultureInfo.Name);

                CurrentlySelectedLanguage = cultureInfo.IetfLanguageTag;
                LanguageResource.Initialize();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public static string GetString(string name, params object[] strs)
        {
            string result = LanguageResource.ResourceManager.GetString(name);
            return !string.IsNullOrEmpty(result) ? string.Format(result, strs) : name;
        }
    }

    public class LanguageResource
    {
        public static ResourceManager ResourceManager;

        public static void Initialize()
        {
            ResourceManager = new ResourceManager(
                $"Comet.Launcher.Localization.Language_{LocaleManager.CurrentlySelectedLanguage}",
                Assembly.GetExecutingAssembly());
        }
    }

    public class Languages
    {
        public string LanguageFullName { get; set; }
        public string LanguageCultureName { get; set; }
    }
}
