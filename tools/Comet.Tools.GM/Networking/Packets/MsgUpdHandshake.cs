using System;
using System.Threading.Tasks;
using Comet.Network.Packets.Updater;
using Comet.Tools.GM.States;
using Org.BouncyCastle.Math;

namespace Comet.Tools.GM.Networking.Packets
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

        public override async Task ProcessAsync(Client client)
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
        }
    }
}
