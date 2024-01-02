namespace Comet.Launcher.Helpers
{
    public static class OperatingSystemHelper
    {
        private static readonly OperatingSystem OS = Environment.OSVersion;
        private static readonly Version WinVersion = OS.Version;

        public static string OperatingSystemName
        {
            get
            {
                //Variable to hold our return value
                var operatingSystem = "";

                if (OS.Platform == PlatformID.Win32Windows)
                    //This is a pre-NT version of Windows
                    switch (WinVersion.Minor)
                    {
                        case 0:
                            operatingSystem = "95";
                            break;
                        case 10:
                            if (WinVersion.Revision.ToString() == "2222A")
                                operatingSystem = "98SE";
                            else
                                operatingSystem = "98";
                            break;
                        case 90:
                            operatingSystem = "Me";
                            break;
                    }
                else if (OS.Platform == PlatformID.Win32NT)
                    switch (WinVersion.Major)
                    {
                        case 3:
                            operatingSystem = "NT 3.51";
                            break;
                        case 4:
                            operatingSystem = "NT 4.0";
                            break;
                        case 5:
                            if (WinVersion.Minor == 0)
                                operatingSystem = "2000";
                            else
                                operatingSystem = "XP";
                            break;
                        case 6:
                            if (WinVersion.Minor == 0)
                                operatingSystem = "Vista";
                            else if (WinVersion.Minor == 1)
                                operatingSystem = "7";
                            else if (WinVersion.Minor == 2)
                                operatingSystem = "8";
                            else
                                operatingSystem = "8.1";
                            break;
                        case 10:
                            operatingSystem = "10";
                            break;
                    }

                //Make sure we actually got something in our OS check
                //We don't want to just return " Service Pack 2" or " 32-bit"
                //That information is useless without the OS version.
                if (!string.IsNullOrEmpty(operatingSystem))
                {
                    //Got something.  Let's prepend "Windows" and get more info.
                    operatingSystem = "Windows " + operatingSystem;
                    //See if there's a service pack installed.
                    if (OS.ServicePack != "")
                        //Append it to the OS name.  i.e. "Windows XP Service Pack 3"
                        operatingSystem += " " + OS.ServicePack;
                    //Append the OS architecture.  i.e. "Windows XP Service Pack 3 32-bit"
                    operatingSystem += " " + (Environment.Is64BitOperatingSystem ? "64" : "32") + "-bit";
                }

                //Return the information we've gathered.
                return operatingSystem;
            }
        }

        public static bool IsWindows7()
        {
            return WinVersion.Major == 6 && WinVersion.Minor == 1;
        }

        public static bool IsWindows7OrHigher()
        {
            return OperatingSystem.IsWindowsVersionAtLeast(6, 1);
        }
    }
}