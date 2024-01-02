using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Comet.Launcher.Server.States;
using Comet.Network.Packets.Updater;
using Org.BouncyCastle.Math;

namespace Comet.Launcher.Server.Packets
{
    internal sealed class MsgUpdHandshake : MsgUpdHandshake<Client>
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
        public override async Task ProcessAsync(Client client)
        {
            if (!client.DiffieHellman.Initialize(PublicKey, Modulus))
            {
                throw new Exception("Couldn't initialize Diffie-Hellman!!!");
            }

            byte[] iv = RandomNumberGenerator.GetBytes(16);
            await client.SendAsync(new MsgUpdHandshake(client.DiffieHellman.PublicKey, client.DiffieHellman.Modulus, iv, iv));

            client.Cipher.GenerateKeys(new object[]
            {
                client.DiffieHellman.SharedKey.ToByteArrayUnsigned(),
                iv,
                iv
            });
        }
    }
}
