using System.Globalization;
using Comet.Shared;
using Microsoft.Extensions.Configuration;

namespace Comet.Launcher.Configuration
{
    public sealed class UserConfiguration
    {
        public const int MINIMUM_FPS_I = 30;
        public const int DEFAULT_FPS_I = 40;
        public const int MAXIMUM_FPS_I = 120;

        public const int MINIMUM_WIDTH_I = 1024;
        public const int MINIMUM_HEIGHT_I = 768;

        private string FilePath => Path.Combine(FrmMain.DataPath, "AutoPatchUserConfig.xml");

        private readonly XmlParser mParser;
        private readonly bool mLoaded;

        public UserConfiguration()
        {
            new ConfigurationBuilder()
                .AddXmlFile(FilePath)
                .Build()
                .Bind(this);

            mParser = new XmlParser(FilePath);
            mLoaded = true;
        }

        private int mFramesPerSecond;

        public int FramesPerSecond
        {
            get => mFramesPerSecond;
            set
            {
                mFramesPerSecond = Math.Min(Math.Max(value, MINIMUM_FPS_I), MAXIMUM_FPS_I);
                if (mLoaded)
                {
                    if (mParser.CheckNodeExists("Config", "FramesPerSecond"))
                    {
                        mParser.ChangeValue(mFramesPerSecond.ToString(CultureInfo.InvariantCulture), "Config",
                                            "FramesPerSecond");
                    }
                    else
                    {
                        mParser.AddNewNode(mFramesPerSecond.ToString(CultureInfo.InvariantCulture), null,
                                           "FramesPerSecond", "Config");
                    }
                }
            }
        }

        private UserConfigurationResolution mResolution;

        public UserConfigurationResolution Resolution
        {
            get => mResolution;
            set
            {
                mResolution = value;
                mResolution.Width = Math.Max(MINIMUM_WIDTH_I, Math.Min(MaxScreenWidth, mResolution.Width));
                mResolution.Height = Math.Max(MINIMUM_WIDTH_I, Math.Min(MaxScreenHeight, mResolution.Height));
                if (mLoaded && value != null)
                {
                    if (mParser.CheckNodeExists("Config", "Resolution"))
                    {
                        mParser.ChangeValue(mResolution.Width.ToString(CultureInfo.InvariantCulture), "Config",
                                            "Resolution", "Width");
                        mParser.ChangeValue(mResolution.Height.ToString(CultureInfo.InvariantCulture), "Config",
                                            "Resolution", "Height");
                    }
                    else
                    {
                        mParser.AddNewNode(mResolution.Width.ToString(CultureInfo.InvariantCulture), null, "Width",
                                           "Config", "Resolution");
                        mParser.AddNewNode(mResolution.Height.ToString(CultureInfo.InvariantCulture), null, "Height",
                                           "Config", "Resolution");
                    }
                }
            }
        }

        private DateTime? mWindowsDefendAlert;

        public DateTime? WindowsDefendAlert
        {
            get => mWindowsDefendAlert;
            set
            {
                mWindowsDefendAlert = value;
                if (mLoaded && value.HasValue)
                {
                    if (mParser.CheckNodeExists("Config", "WindowsDefendAlert"))
                    {
                        mParser.ChangeValue(mWindowsDefendAlert.Value.ToString(CultureInfo.InvariantCulture), "Config",
                                            "WindowsDefendAlert");
                    }
                    else
                    {
                        mParser.AddNewNode(mWindowsDefendAlert.Value.ToString(CultureInfo.InvariantCulture), null,
                                           "WindowsDefendAlert", "Config");
                    }
                }
            }
        }

        public int MaxScreenWidth => Screen.PrimaryScreen.Bounds.Width;
        public int MaxScreenHeight => Screen.PrimaryScreen.Bounds.Height;

        private bool mSuppressMinimizeAlert;

        public bool SuppressMinimizeAlert
        {
            get => mSuppressMinimizeAlert;
            set
            {
                mSuppressMinimizeAlert = value;
                if (mLoaded)
                {
                    if (mParser.CheckNodeExists("Config", "SupressMinimizeAlert"))
                    {
                        mParser.ChangeValue((mSuppressMinimizeAlert ? 1 : 0).ToString(CultureInfo.InvariantCulture),
                                            "Config", "SupressMinimizeAlert");
                    }
                    else
                    {
                        mParser.AddNewNode((mSuppressMinimizeAlert ? 1 : 0).ToString(CultureInfo.InvariantCulture),
                                           null, "SupressMinimizeAlert", "Config");
                    }
                }
            }
        }

        private bool mSuppressIncorrectWindowsVersion;

        public bool SuppressIncorrectWindowsVersion
        {
            get => mSuppressIncorrectWindowsVersion;
            set
            {
                mSuppressIncorrectWindowsVersion = value;
                if (mLoaded)
                {
                    if (mParser.CheckNodeExists("Config", "SupressMinimizeAlert"))
                    {
                        mParser.ChangeValue(
                            (mSuppressIncorrectWindowsVersion ? 1 : 0).ToString(CultureInfo.InvariantCulture), "Config",
                            "SuppressIncorrectWindowsVersion");
                    }
                    else
                    {
                        mParser.AddNewNode(
                            (mSuppressIncorrectWindowsVersion ? 1 : 0).ToString(CultureInfo.InvariantCulture), null,
                            "SuppressIncorrectWindowsVersion", "Config");
                    }
                }
            }
        }

        public void Save()
        {
            mParser?.Save();
        }
    }

    public class UserConfigurationResolution
    {
        public int Width { get; set; }
        public int Height { get; set; }
    }
}