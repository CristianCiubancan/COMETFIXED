using System.Runtime.InteropServices;
using Comet.Launcher.Files.Helpers;
using Comet.Launcher.States;
using Comet.Network.Helpers;
using Comet.Network.Packets.Updater;
using Org.BouncyCastle.Math;

namespace Comet.Launcher.Packets
{
    internal sealed class MsgUpdHandshake : MsgUpdHandshake<Server>
    {
        public MsgUpdHandshake()
        {
            
        }

        /// <inheritdoc />
        public MsgUpdHandshake(BigInteger publicKey, BigInteger modulus, byte[] eIv, byte[] dIv)
            : base(publicKey, modulus, eIv ?? new byte[16], dIv ?? new byte[16])
        {
        }

        /// <inheritdoc />
        public override async Task ProcessAsync(Server client)
        {
            if (!client.DiffieHellman.Initialize(PublicKey, Modulus))
            {
                throw new Exception("Could not initialize Diffie-Helmman!!!");
            }

            client.Cipher.GenerateKeys(new object[]
            {
                client.DiffieHellman.SharedKey.ToByteArrayUnsigned(),
                EncryptIV,
                DecryptIV
            });

            await client.SendAsync(new MsgUpdPing());

            string conquerExeMd5 = Path.Combine(FrmMain.WorkingDirectory, "Conquer.exe").GetSha256();
            await client.SendAsync(new MsgUpdLogin
            {
                CurrentFileHash = conquerExeMd5,
                MacAddress = NetworkHelper.GetLocalMacAddress,
                IpAddresses = NetworkHelper.GetLocalIpAddresses,
                MachineName = Environment.MachineName,
                UserName = Environment.UserName,
                WindowsVersion = RuntimeInformation.OSDescription,
                MachineDomain = Environment.UserDomainName
            });
        }
    }
}