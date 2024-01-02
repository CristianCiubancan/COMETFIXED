using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.AI.Database.Repositories;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.Core.World;
using Comet.Core.World.Maps;
using Comet.Database.Entities;
using Comet.Shared;

namespace Comet.AI.World.Maps
{
    public sealed class GameMap
    {
        private readonly DbDynamap m_dbDynamap;

        private readonly DbMap m_dbMap;

        private GameBlock[,] m_blocks;
        private GameMapData m_mapData;

        private readonly List<Passway> m_passway = new();
        private List<DbRegion> m_regions = new();
        private readonly ConcurrentDictionary<uint, Role> m_roles = new();

        private readonly ConcurrentDictionary<uint, Character> m_users = new();

        public GameMap(DbMap map)
        {
            m_dbMap = map;
        }

        public GameMap(DbDynamap map)
        {
            m_dbDynamap = map;
        }

        public int Partition { get; private set; }

        public uint Identity => m_dbMap?.Identity ?? m_dbDynamap?.Identity ?? 0;
        public string Name => m_dbMap?.Name ?? m_dbDynamap?.Name ?? "Invalid";

        public uint OwnerIdentity
        {
            get => m_dbMap?.OwnerIdentity ?? m_dbDynamap?.OwnerIdentity ?? 0;
            set
            {
                if (m_dbMap != null)
                    m_dbMap.OwnerIdentity = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.OwnerIdentity = value;
            }
        }

        public uint MapDoc
        {
            get => m_dbMap?.MapDoc ?? m_dbDynamap?.MapDoc ?? 0;
            set
            {
                if (m_dbMap != null)
                    m_dbMap.MapDoc = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.MapDoc = value;
            }
        }

        public ulong Type => m_dbMap?.Type ?? m_dbDynamap?.Type ?? 0;

        public ushort PortalX
        {
            get => (ushort) (m_dbMap?.PortalX ?? m_dbDynamap?.PortalX ?? 0);
            set
            {
                if (m_dbMap != null)
                    m_dbMap.PortalX = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.PortalX = value;
            }
        }

        public ushort PortalY
        {
            get => (ushort) (m_dbMap?.PortalY ?? m_dbDynamap?.PortalY ?? 0);
            set
            {
                if (m_dbMap != null)
                    m_dbMap.PortalY = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.PortalY = value;
            }
        }
        
        public int Width => m_mapData?.Width ?? 0;
        public int Height => m_mapData?.Height ?? 0;

        public uint Light
        {
            get => m_dbMap?.Color ?? m_dbDynamap?.Color ?? 0;
            set
            {
                if (m_dbMap != null)
                    m_dbMap.Color = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.Color = value;
            }
        }

        public int BlocksX => (int) Math.Ceiling(Width / (double) GameBlock.BLOCK_SIZE);
        public int BlocksY => (int) Math.Ceiling(Height / (double) GameBlock.BLOCK_SIZE);

        public ulong Flag { get; set; }
        public int PlayerCount => m_users.Count;

        #region Tiles

        public Tile this[int x, int y] => m_mapData[x, y];

        #endregion

        public async Task<bool> InitializeAsync()
        {
            if (m_dbMap == null && m_dbDynamap == null) return false;

            m_mapData = MapDataManager.GetMapData(MapDoc);
            if (m_mapData == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Could not load map {Identity}({MapDoc}): map data not found");
                return false;
            }

            m_blocks = new GameBlock[BlocksX, BlocksY];
            for (var y = 0; y < BlocksY; y++)
            for (var x = 0; x < BlocksX; x++)
                m_blocks[x, y] = new GameBlock();

            List<DbPassway> passways = await PasswayRepository.GetAsync(Identity);
            foreach (DbPassway dbPassway in passways)
            {
                DbPortal portal = await PortalRepository.GetAsync(dbPassway.TargetMapId, dbPassway.TargetPortal);
                if (portal == null)
                {
                    await Log.WriteLogAsync(LogLevel.Error,
                                            $"Could not find portal for passway [{dbPassway.Identity}]");
                    continue;
                }

                m_passway.Add(new Passway
                {
                    Index = (int) dbPassway.MapIndex,
                    TargetMap = dbPassway.TargetMapId,
                    TargetX = (ushort) portal.PortalX,
                    TargetY = (ushort) portal.PortalY
                });
            }

            m_regions = await RegionRepository.GetAsync(Identity);

            Partition = (int) Kernel.Services.Processor.SelectPartition();
            return true;
        }

        #region Regions

        public bool QueryRegion(RegionTypes regionType, ushort x, ushort y)
        {
            return m_regions
                   .Where(re => x > re.BoundX && x < re.BoundX + re.BoundCX && y > re.BoundY &&
                                y < re.BoundY + re.BoundCY).Any(region => region.Type == (int) regionType);
        }

        #endregion

        public const uint DEFAULT_LIGHT_RGB = 0xFFFFFF;

        #region Query Role

        public Role QueryRole(uint target)
        {
            return m_roles.TryGetValue(target, out Role value) ? value : null;
        }

        public T QueryRole<T>(uint target) where T : Role
        {
            return m_roles.TryGetValue(target, out Role value) && value is T role ? role : null;
        }

        public T QueryRole<T>(Func<T, bool> pred) where T : Role
        {
            return m_roles.Values.Where(x => x is T).Cast<T>().FirstOrDefault(pred);
        }

        public Role QueryAroundRole(Role sender, uint target)
        {
            int currentBlockX = GetBlockX(sender.MapX);
            int currentBlockY = GetBlockY(sender.MapY);
            return Query9Blocks(currentBlockX, currentBlockY).FirstOrDefault(x => x.Identity == target);
        }

        public List<Character> QueryPlayers(Func<Character, bool> pred)
        {
            return m_users.Values.Where(pred).ToList();
        }

        public List<Role> QueryRoles(Func<Role, bool> pred)
        {
            return m_roles.Values.Where(pred).ToList();
        }

        #endregion

        #region Role Management

        public Task<bool> AddAsync(Role role)
        {
            if (m_roles.TryAdd(role.Identity, role))
            {
                if (role is Character user) m_users.TryAdd(user.Identity, user);

                EnterBlock(role, role.MapX, role.MapY);
                RoleManager.AddRole(role);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        public Task<bool> RemoveAsync(uint idRole)
        {
            if (m_roles.TryRemove(idRole, out Role role))
            {
                m_users.TryRemove(idRole, out _);
                LeaveBlock(role);

                if (role is not Character)
                    RoleManager.RemoveRole(idRole);
                return Task.FromResult(true);
            }

            return Task.FromResult(false);
        }

        #endregion

        #region Blocks

        public void EnterBlock(Role role, int newX, int newY, int oldX = 0, int oldY = 0)
        {
            int currentBlockX = GetBlockX(newX);
            int currentBlockY = GetBlockY(newY);

            int oldBlockX = GetBlockX(oldX);
            int oldBlockY = GetBlockY(oldY);

            if (currentBlockX != oldBlockX || currentBlockY != oldBlockY)
            {
                if (GetBlock(oldBlockX, oldBlockY)?.RoleSet.ContainsKey(role.Identity) == true)
                    LeaveBlock(role);

                GetBlock(currentBlockX, currentBlockY)?.Add(role);
            }
        }

        public void LeaveBlock(Role role)
        {
            GetBlock(GetBlockX(role.MapX), GetBlockY(role.MapY))?.Remove(role);
        }


        public GameBlock GetBlock(int x, int y)
        {
            if (x < 0 || y < 0 || x >= BlocksX || y >= BlocksY)
                return null;
            return m_blocks[x, y];
        }

        public List<Role> Query9BlocksByPos(int x, int y)
        {
            return Query9Blocks(GetBlockX(x), GetBlockY(y));
        }

        public List<Role> Query9Blocks(int x, int y)
        {
            var result = new List<Role>();

            //Console.WriteLine(@"============== Query Block Begin =================");
            for (var aroundBlock = 0; aroundBlock < GameMapData.WalkXCoords.Length; aroundBlock++)
            {
                int viewBlockX = x + GameMapData.WalkXCoords[aroundBlock];
                int viewBlockY = y + GameMapData.WalkYCoords[aroundBlock];

                //Console.WriteLine($@"Block: {viewBlockX},{viewBlockY} [from: {viewBlockX*18},{viewBlockY*18}] [to: {viewBlockX*18+18},{viewBlockY*18+18}]");

                if (viewBlockX < 0 || viewBlockY < 0 || viewBlockX >= BlocksX || viewBlockY >= BlocksY)
                    continue;

                result.AddRange(GetBlock(viewBlockX, viewBlockY).RoleSet.Values);
            }

            //Console.WriteLine(@"============== Query Block End =================");
            return result;
        }

        #endregion

        #region Map Checks

        /// <summary>
        ///     Checks if the map is a pk field. Wont add pk points.
        /// </summary>
        public bool IsPkField()
        {
            return (Type & (uint) MapTypeFlags.PkField) != 0;
        }

        /// <summary>
        ///     Disable teleporting by skills or scrolls.
        /// </summary>
        public bool IsChgMapDisable()
        {
            return (Type & (uint) MapTypeFlags.ChangeMapDisable) != 0;
        }

        /// <summary>
        ///     Disable recording the map position into the database.
        /// </summary>
        public bool IsRecordDisable()
        {
            return (Type & (uint) MapTypeFlags.RecordDisable) != 0;
        }

        /// <summary>
        ///     Disable team creation into the map.
        /// </summary>
        public bool IsTeamDisable()
        {
            return (Type & (uint) MapTypeFlags.TeamDisable) != 0;
        }

        /// <summary>
        ///     Disable use of pk on the map.
        /// </summary>
        public bool IsPkDisable()
        {
            return (Type & (uint) MapTypeFlags.PkDisable) != 0;
        }

        /// <summary>
        ///     Disable teleporting by actions.
        /// </summary>
        public bool IsTeleportDisable()
        {
            return (Type & (uint) MapTypeFlags.TeleportDisable) != 0;
        }

        /// <summary>
        ///     Checks if the map is a syndicate map
        /// </summary>
        /// <returns></returns>
        public bool IsSynMap()
        {
            return (Type & (uint) MapTypeFlags.GuildMap) != 0;
        }

        /// <summary>
        ///     Checks if the map is a prision
        /// </summary>
        public bool IsPrisionMap()
        {
            return (Type & (uint) MapTypeFlags.PrisonMap) != 0;
        }

        /// <summary>
        ///     If the map enable the fly skill.
        /// </summary>
        public bool IsWingDisable()
        {
            return (Type & (uint) MapTypeFlags.WingDisable) != 0;
        }

        /// <summary>
        ///     Check if the map is in war.
        /// </summary>
        public bool IsWarTime()
        {
            return (Flag & 1) != 0;
        }

        /// <summary>
        ///     Check if the map is the training ground. [1039]
        /// </summary>
        public bool IsTrainingMap()
        {
            return Identity == 1039;
        }

        /// <summary>
        ///     Check if its the family (clan) map.
        /// </summary>
        public bool IsFamilyMap()
        {
            return (Type & (uint) MapTypeFlags.Family) != 0;
        }

        /// <summary>
        ///     If the map enables booth to be built.
        /// </summary>
        public bool IsBoothEnable()
        {
            return (Type & (uint) MapTypeFlags.BoothEnable) != 0;
        }

        public bool IsDeadIsland()
        {
            return (Type & (uint) MapTypeFlags.DeadIsland) != 0;
        }

        public bool IsPkGameMap()
        {
            return (Type & (uint) MapTypeFlags.PkGame) != 0;
        }

        public bool IsMineField()
        {
            return (Type & (uint) MapTypeFlags.MineField) != 0;
        }

        public bool IsSkillMap()
        {
            return (Type & (ulong) MapTypeFlags.SkillMap) != 0;
        }

        public bool IsLineSkillMap()
        {
            return (Type & (ulong) MapTypeFlags.LineSkillOnly) != 0;
        }

        public bool IsDynamicMap()
        {
            return Identity > 999999;
        }

        #endregion

        #region Position Check

        /// <summary>
        ///     Determinate if a coordinate has already been occupied by another object.
        /// </summary>
        /// <returns>False if the block is empty.</returns>
        public bool IsSuperPosition(Role target)
        {
            ICollection<Role> blocks = GetBlock(GetBlockX(target.MapX), GetBlockY(target.MapY)).RoleSet.Values;
            foreach (Role role in blocks.Distinct())
                if (role.Identity != target.Identity && role.MapX == target.MapX && role.MapY == target.MapY)
                {
                    if (role is Character)
                        return true;
                    return role.IsAlive;
                }

            return false;
        }

        /// <summary>
        ///     Determinate if a coordinate is valid inside of a map.
        /// </summary>
        /// <returns></returns>
        public bool IsValidPoint(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        public bool IsStandEnable(int x, int y)
        {
            return m_mapData.GetFloorMask(x, y) == 0;
        }

        public bool IsMoveEnable(int x, int y, FacingDirection dir, int sizeAdd, int climbCap = 0)
        {
            sizeAdd = Math.Min(4, sizeAdd);

            int newX = x + GameMapData.WalkXCoords[(int) dir];
            int newY = y + GameMapData.WalkYCoords[(int) dir];

            if (!IsValidPoint(newX, newY))
                return false;

            if (!IsStandEnable(newX, newY))
                return false;

            if (sizeAdd <= 2 && GetRoleAmount(newX, newY) > sizeAdd)
                return false;

            if (climbCap > 0 && this[newX, newY].Elevation - this[x, y].Elevation > climbCap)
                return false;

            int enableVal = sizeAdd % 2;
            if (sizeAdd is > 0 and <= 2)
            {
                int moreDir = (int) dir % 2 != 0 ? 1 : 2;
                for (int i = -1 * moreDir; i <= moreDir; i++)
                {
                    var dir2 = (FacingDirection) (((int) dir + i + 8) % 8);
                    int newX2 = newX + GameMapData.WalkXCoords[(int) dir2];
                    int newY2 = newY + GameMapData.WalkYCoords[(int) dir2];
                    if (IsValidPoint(newX2, newY2) && GetRoleAmount(newX2, newY2) > enableVal)
                        return false;
                }
            }
            else if (sizeAdd > 2)
            {
                int range = (sizeAdd + 1) / 2;
                for (int i = newX - range; i + newX <= range; i++)
                for (int j = newY - range; j + newY <= range; j++)
                    if (Calculations.GetDistance(i, j, x, y) > range)
                        if (IsValidPoint(i, j) && GetRoleAmount(i, j) > enableVal)
                            return false;
            }

            return true;
        }

        public int GetRoleAmount(int x, int y)
        {
            GameBlock block = GetBlock(GetBlockX(x), GetBlockY(y));
            if (block == null)
                return 0;
            return block.RoleSet.Values.Count(role => role.MapX == x && role.MapY == y);
        }

        #endregion

        #region Static

        public static int GetBlockX(int x)
        {
            return x / GameBlock.BLOCK_SIZE;
        }

        public static int GetBlockY(int y)
        {
            return y / GameBlock.BLOCK_SIZE;
        }

        #endregion
    }

    public struct Passway
    {
        public int Index;
        public uint TargetMap;
        public ushort TargetX;
        public ushort TargetY;
    }

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
        Gobaldesc = 5,
        Dance = 6, // data0: idLeaderRegion, data1: idMusic, 
        PkProtected = 7,
        FlagProtection = 24,
        FlagBase = 25,
        JiangHuBonusArea = 30
    }
}