using System;
using Comet.Database.Entities;
using Comet.Shared;

namespace Comet.AI.States
{
    public sealed class MonsterMagic
    {
        private readonly TimeOutMS timeOutMS = new();

        public MonsterMagic(DbMonsterTypeMagic magic)
        {
            MonsterType = magic.MonsterType;
            MagicType = magic.MagicType;
            MagicLev = magic.MagicLev;
            ColdTime = magic.ColdTime;
            WarningTime = magic.WarningTime;
            StatusMagicLev = magic.StatusMagicLev;
            StatusMagicType = magic.StatusMagicType;
            LastTick = 0;

            timeOutMS.Startup((int) ColdTime);
        }

        public uint MonsterType { get; }
        public uint MagicType { get; }
        public uint MagicLev { get; }
        public uint ColdTime { get; }
        public ushort WarningTime { get; }
        public uint StatusMagicType { get; }
        public uint StatusMagicLev { get; }
        public long LastTick { get; set; }

        public bool IsReady()
        {
            return timeOutMS.IsTimeOut();
        }

        public void Use()
        {
            timeOutMS.Update();
            LastTick = Environment.TickCount64;
        }
    }
}