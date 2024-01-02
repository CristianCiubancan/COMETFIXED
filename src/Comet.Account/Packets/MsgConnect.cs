using System;
using System.Threading.Tasks;
using Comet.Account.States;
using Comet.Network.Packets;

namespace Comet.Account.Packets
{
    // <remarks>Packet Type 1052</remarks>
    /// <summary>
    ///     Message containing a connection request to the game server. Contains the player's
    ///     access token from the Account server, and the patch and language versions of the
    ///     game client.
    /// </summary>
    public sealed class MsgConnect : MsgBase<Client>
    {
        // Static properties from server initialization
        public static bool StrictAuthentication { get; set; }

        // Packet Properties
        public ulong Token { get; set; }
        public ushort Patch { get; set; }
        public string Language { get; set; }
        public int Version { get; set; }

        /// <summary>
        ///     Decodes a byte packet into the packet structure defined by this message class.
        ///     Should be invoked to structure data from the client for processing. Decoding
        ///     follows TQ Digital's byte ordering rules for an all-binary protocol.
        /// </summary>
        /// <param name="bytes">Bytes from the packet processor or client socket</param>
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Token = reader.ReadUInt64();
            Patch = reader.ReadUInt16();
            Language = reader.ReadString(10);
            Version = Convert.ToInt32(reader.ReadInt32().ToString(), 2);
        }

        public override Task ProcessAsync(Client client)
        {
            var a = (byte) (Patch >> 8);
            var b = (byte) Patch;

            return base.ProcessAsync(client);
        }
    }
}