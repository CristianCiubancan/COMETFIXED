using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgTaskStatus<T> : MsgBase<T>
    {
        public List<TaskItemStruct> Tasks = new();
        public TaskStatusMode Mode { get; set; }
        public ushort Amount { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();
            Type = (PacketType) reader.ReadUInt16();
            Mode = (TaskStatusMode) reader.ReadUInt16();
            Amount = reader.ReadUInt16();
            for (var i = 0; i < Amount; i++)
            {
                var item = new TaskItemStruct
                {
                    Identity = reader.ReadInt32(),
                    Status = (TaskItemStatus) reader.ReadInt32()
                };
                Tasks.Add(item);
            }
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgTaskStatus);
            writer.Write((ushort) Mode);
            writer.Write(Amount = (ushort) Tasks.Count);
            foreach (TaskItemStruct task in Tasks)
            {
                writer.Write(task.Identity);
                writer.Write((int) task.Status);
                // writer.Write(task.Unknown);
            }

            return writer.ToArray();
        }

        public class TaskItemStruct
        {
            public int Identity { get; set; }
            public TaskItemStatus Status { get; set; }
            public int Unknown { get; set; }
        }

        public enum TaskItemStatus : byte
        {
            Accepted = 0,
            Done = 1,
            Available = 2,
            Event = 3,
            Daily = 4,
            AcceptedWithoutTrace = 5,
            Quitted = 6,
            None = 255
        }

        public enum TaskStatusMode : byte
        {
            None = 0,
            Add = 1,
            Remove = 2,
            Update = 3,
            Finish = 4,
            Quit = 8
        }
    }
}