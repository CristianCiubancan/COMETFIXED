#define LOCAL_RELEASE
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Comet.Launcher.Files.Helpers;
using Comet.Launcher.Server.States;
using Comet.Network.Helpers;
using Comet.Network.Packets.Updater;

namespace Comet.Launcher.Server.Packets
{
    internal sealed class MsgUpdLogin : MsgUpdLogin<Client>
    {
        /// <inheritdoc />
        public override async Task ProcessAsync(Client client)
        {
            if (!NetworkHelper.IsValidMacAddress(MacAddress))
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.InvalidMacAddress
                });
                client.Disconnect();
                return;
            }

            if (IpAddresses.Count <= 0)
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.InvalidIpAddress
                });
                client.Disconnect();
                return;
            }

#if !DEBUG && !LOCAL_RELEASE
            if (IpAddresses.TrueForAll(x => !client.IpAddress.Equals(x)))
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.InvalidIpAddress
                });
                client.Disconnect();
                return;
            }
#else
            if (!(client.IpAddress.StartsWith("127.")
                  || client.IpAddress.Equals("localhost")
                  || IpAddresses.Any(x => x.Equals(client.IpAddress))))
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.InvalidIpAddress
                });
                client.Disconnect();
                return;
            }
#endif

            // client validation
            if (Kernel.Clients.Values.Any(x => x.MacAddress.Equals(MacAddress)))
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.MacAddressAlreadySignedIn
                });
                client.Disconnect();
                return;
            }

            if (Kernel.Clients.Values.Any(x => x.MachineDomain.Equals(MachineDomain) &&
                                               x.MachineName.Equals(MachineName)))
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.ComputerAlreadySignedIn
                });
                client.Disconnect();
                return;
            }

            if (Kernel.Clients.TryGetValue(client.GUID, out _))
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.GuidHacking
                });
                client.Disconnect();
                return;
            }

            // Conquer validation will be made after client is ready
            //string conquerPath = Path.Combine(Environment.CurrentDirectory, "Conquer.exe");
            //if (File.Exists(conquerPath)
            //    && !conquerPath.GetMd5().Equals(CurrentFileMd5))
            //{
            //    await client.SendAsync(new MsgUpdLoginEx
            //    {
            //        Response = UpdLoginEx.InvalidConquerMd5
            //    });
            //    client.Disconnect();
            //    return;
            //}

            client.CurrentFileMd5 = CurrentFileHash;
            client.UserName = UserName;
            client.MacAddress = MacAddress;
            client.MachineDomain = MachineDomain;
            client.IpAddresses = new List<string>(IpAddresses);
            client.WindowsVersion = WindowsVersion;
            client.MachineName = MachineName;

            if (Kernel.Clients.TryAdd(client.GUID, client))
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.Success
                });
            }
            else
            {
                await client.SendAsync(new MsgUpdLoginEx
                {
                    Response = UpdLoginEx.GuidHacking
                });
            }
        }
    }
}