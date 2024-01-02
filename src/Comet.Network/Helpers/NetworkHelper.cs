using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace Comet.Network.Helpers
{
    public static class NetworkHelper
    {
        public static List<string> GetLocalIpAddresses => Dns.GetHostEntry(Dns.GetHostName()).AddressList
                                                             .Where(x => x.AddressFamily == AddressFamily.InterNetwork).Select(x => x.ToString()).ToList();

        public static string GetLocalMacAddress => NetworkInterface.GetAllNetworkInterfaces()
                                                                   .Where(n => n.OperationalStatus == OperationalStatus.Up &&
                                                                               n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                                                                   .OrderByDescending(n => n.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                                                                   .Select(n => n.GetPhysicalAddress())
                                                                   .FirstOrDefault()?.ToString();

        /// <summary>
        ///     Determines whether if input is a valid MAC address.
        /// </summary>
        /// <param name="macAddress">
        ///     MAC address as string to convert. Possible format is 000000000000, 00 00 00 00 00 00,
        ///     00:00:00:00:00:00, 00-00-00-00-00-00
        /// </param>
        /// <returns>True if MAC address is valid, false otherwise</returns>
        public static bool IsValidMacAddress(string macAddress)
        {
            bool result;
            Regex rxMacAddress;

            if (string.IsNullOrEmpty(macAddress) || macAddress.Length is < 12 or > 17)
            {
                result = false;
            }
            else if (macAddress.Length == 12)
            {
                rxMacAddress = new Regex(@"^[0-9a-fA-F]{12}$");
                result = rxMacAddress.IsMatch(macAddress);
            }
            else
            {
                rxMacAddress = new Regex(@"^([0-9A-F]{2}[:-]){5}([0-9A-F]{2})$");
                result = rxMacAddress.IsMatch(macAddress);
            }

            return result;
        }
    }
}
