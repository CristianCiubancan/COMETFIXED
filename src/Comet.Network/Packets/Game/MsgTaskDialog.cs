using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgTaskDialog<T> : MsgBase<T>
    {
        protected MsgTaskDialog()
        {
            Text = string.Empty;
        }

        public uint TaskIdentity { get; set; }
        public ushort Data { get; set; }
        public byte OptionIndex { get; set; }
        public TaskInteraction InteractionType { get; set; }
        public string Text { get; set; }

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
            TaskIdentity = reader.ReadUInt32();
            Data = reader.ReadUInt16();
            OptionIndex = reader.ReadByte();
            InteractionType = (TaskInteraction) reader.ReadByte();
            List<string> strings = reader.ReadStrings();
            Text = strings.Count > 0 ? strings[0] : "";
        }

        /// <summary>
        ///     Encodes the packet structure defined by this message class into a byte packet
        ///     that can be sent to the client. Invoked automatically by the client's send
        ///     method. Encodes using byte ordering rules interoperable with the game client.
        /// </summary>
        /// <returns>Returns a byte packet of the encoded packet.</returns>
        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTaskDialog);
            writer.Write(TaskIdentity);
            writer.Write(Data);
            writer.Write(OptionIndex);
            writer.Write((byte) InteractionType);
            writer.Write(new List<string> {Text});
            return writer.ToArray();
        }

        public enum TaskInteraction : byte
        {
            ClientRequest = 0,
            Dialog = 1,
            Option = 2,
            Input = 3,
            Avatar = 4,
            LayNpc = 5,
            MessageBox = 6,
            Finish = 100,
            Answer = 101,
            TextInput = 102,
            UpdateWindow = 112
        }
    }
}