using System.Collections.Generic;

namespace Comet.Network.Packets.Updater
{
    public abstract class MsgUpdPatchList<T> : MsgBase<T>
    {
        public MsgUpdPatchType Mode { get; set; }
        public string Domain { get; set; }
        public int Count { get; set; }
        public List<UpdatePatch> Patches { get; } = new();

        /// <inheritdoc />
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgUpdPatchList);
            writer.Write((int) Mode);
            writer.Write(Domain);
            writer.Write(Count = Patches.Count);
            foreach (UpdatePatch patch in Patches)
            {
                writer.Write(patch.Version);
                writer.Write(patch.FileName, 16);
                writer.Write(patch.Hash);
            }

            return writer.ToArray();
        }

        /// <inheritdoc />
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Mode = (MsgUpdPatchType) reader.ReadInt32();
            Domain = reader.ReadString();
            Count = reader.ReadInt32();
            for (var i = 0; i < Count; i++)
            {
                int version = reader.ReadInt32();
                string fileName = reader.ReadString(16);
                string hash = reader.ReadString();
                Patches.Add(new UpdatePatch
                {
                    Version = version,
                    FileName = fileName,
                    Hash = hash
                });
            }
        }
    }

    public struct UpdatePatch
    {
        public int Version { get; init; }
        public string FileName { get; init; }
        public string Hash { get; init; }
    }

    public enum MsgUpdPatchType
    {
        Request,
        Client,
        Game,
        NoUpdate
    }
}