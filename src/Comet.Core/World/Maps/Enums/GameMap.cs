using System;

namespace Comet.Core.World.Maps.Enums
{
    [Flags]
    public enum MapTypeFlags : ulong
    {
        Normal = 0,
        PkField = 0x1,          //0x1 1
        ChangeMapDisable = 0x2, //0x2 2
        RecordDisable = 0x4,    //0x4 4 
        PkDisable = 0x8,        //0x8 8
        BoothEnable = 0x10,     //0x10 16
        TeamDisable = 0x20,     //0x20 32
        TeleportDisable = 0x40, // 0x40 64
        GuildMap = 0x80,        // 0x80 128
        PrisonMap = 0x100,      // 0x100 256
        WingDisable = 0x200,    // 0x200 512
        Family = 0x400,         // 0x400 1024
        MineField = 0x800,      // 0x800 2048
        PkGame = 0x1000,        // 0x1000 4098
        NeverWound = 0x2000,    // 0x2000 8196
        DeadIsland = 0x4000,    // 0x4000 16392
        SkillMap = 1UL << 62,
        LineSkillOnly = 1UL << 63
    }

    public enum RegionTypes
    {
        None = 0,
        City = 1,
        Weather = 2,
        Statuary = 3,
        Desc = 4,
        GlobalDesc = 5,
        Dance = 6, // data0: idLeaderRegion, data1: idMusic, 
        PkProtected = 7,
        FlagProtection = 24,
        FlagBase = 25,
        FlagSpawnArea = 26,
        JiangHuBonusArea = 30
    }
}