using System.Collections.Generic;

namespace Comet.Network.Packets.Updater
{
    public abstract class MsgUpdCheckHash<T> : MsgBase<T>
    {
        public bool Eof { get; set; }
        public int Count { get; private set; }
        public List<FileHash> Hashes { get; set; } = new();

        /// <inheritdoc />
        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Eof = reader.ReadBoolean();
            Count = reader.ReadInt32();
            for (var i = 0; i < Count; i++)
            {
                string path = reader.ReadString();
                string hash = reader.ReadString();
                Hashes.Add(new FileHash
                {
                    FilePath = path,
                    Hash = hash
                });
            }
        }

        /// <inheritdoc />
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgUpdCheckHash);
            writer.Write(Eof);
            writer.Write(Count = Hashes.Count);
            foreach (FileHash hash in Hashes)
            {
                writer.Write(hash.FilePath);
                writer.Write(hash.Hash);
            }

            return writer.ToArray();
        }

        public int CalculateMsgSize()
        {
            var size = 9;
            foreach (FileHash hash in Hashes)
            {
                size += 2;
                size += hash.FilePath.Length;
                size += hash.Hash.Length;
            }

            return size;
        }

        public struct FileHash
        {
            public string FilePath { get; set; }
            public string Hash { get; set; }
        }
    }
}