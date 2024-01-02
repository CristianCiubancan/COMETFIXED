using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets.Game;
using Comet.Network.Security;

namespace Comet.Game.Packets
{
    /// <summary>
    ///     Message containing keys necessary for conducting the Diffie-Hellman key exchange.
    ///     The initial message to the client is sent on connect, and contains initial seeds
    ///     for Blowfish. The response message from the client then contains the shared key.
    /// </summary>
    public sealed class MsgHandshake : MsgHandshake<Client>
    {
        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgHandshake" />. This constructor
        ///     is called to accept the client response.
        /// </summary>
        public MsgHandshake()
        {
        }

        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgHandshake" />. This constructor
        ///     is called to construct the initial request to the client.
        /// </summary>
        /// <param name="dh">Diffie-Hellman key exchange instance for the actor</param>
        /// <param name="encryptionIV">Initial seed for Blowfish's encryption IV</param>
        /// <param name="decryptionIV">Initial seed for Blowfish's decryption IV</param>
        public MsgHandshake(NDDiffieHellman dh, byte[] encryptionIV, byte[] decryptionIV)
            : base(dh, encryptionIV, decryptionIV)
        {
        }

        /// <summary>Randomizes padding for the message.</summary>
        public async Task RandomizeAsync()
        {
            Padding = new byte[await Kernel.NextAsync(24, 48)];
            await Kernel.NextBytesAsync(Padding);
        }
    }
}