using System.Collections.Generic;

namespace Comet.Network.Packets.Game
{
    public abstract class MsgQuiz<T> : MsgBase<T>
    {
        public List<QuizRank> Scores = new();

        public List<string> Strings = new();
        public QuizAction Action { get; set; }

        /// <remarks>Countdown | Score | Question Number</remarks>
        public ushort Param1 { get; set; }

        /// <remarks>Last Correct Answer | Time Taken | Reward</remarks>
        public ushort Param2 { get; set; }

        /// <remarks>Time Per Question | Exp. Awarded |  Rank</remarks>
        public ushort Param3 { get; set; }

        /// <remarks>First Prize | Time Taken</remarks>
        public ushort Param4 { get; set; }

        /// <remarks>Second Prize | Current Score</remarks>
        public ushort Param5 { get; set; }

        /// <remarks>Third Prize</remarks>
        public ushort Param6 { get; set; }

        public ushort Param7 { get; set; }
        public ushort Param8 { get; set; }
        public ushort Param9 { get; set; }

        public override void Decode(byte[] bytes)
        {
            var reader = new PacketReader(bytes);
            Length = reader.ReadUInt16();            // 0
            Type = (PacketType) reader.ReadUInt16(); // 2
            Action = (QuizAction) reader.ReadUInt16();
            Param1 = reader.ReadUInt16();
            Param2 = reader.ReadUInt16();
            Param3 = reader.ReadUInt16();
            Param4 = reader.ReadUInt16();
            Param5 = reader.ReadUInt16();
            Param6 = reader.ReadUInt16();
            Param7 = reader.ReadUInt16();
            Param8 = reader.ReadUInt16();
            Param9 = reader.ReadUInt16();
            Strings = reader.ReadStrings();
        }

        public override byte[] Encode()
        {
            var writer = new PacketWriter();
            writer.Write((ushort) PacketType.MsgQuiz);
            writer.Write((ushort) Action);
            writer.Write(Param1);
            writer.Write(Param2);
            writer.Write(Param3);
            writer.Write(Param4);
            writer.Write(Param5);
            writer.Write(Param6);
            writer.Write(Param7);
            if (Scores.Count > 0)
            {
                writer.Write(Scores.Count);
                foreach (QuizRank score in Scores)
                {
                    writer.Write(score.Name, 16);
                    writer.Write(score.Score);
                    writer.Write(score.Time);
                }
            }
            else
            {
                writer.Write(Param8);
                writer.Write(Param9);
                writer.Write(Strings);
            }

            return writer.ToArray();
        }

        public struct QuizRank
        {
            public string Name { get; set; }
            public ushort Score { get; set; }
            public ushort Time { get; set; }
        }

        public enum QuizAction : ushort
        {
            None,
            Start,
            Question,
            Reply,
            AfterReply,
            Finish,
            Quit = 8
        }
    }
}