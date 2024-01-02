using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core.World;
using Comet.Core.World.Maps;
using Comet.Core.World.Maps.Enums;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Internal.AI;
using Comet.Game.Packets;
using Comet.Game.Packets.Ai;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Ai;
using Comet.Network.Packets.Game;
using Comet.Shared;
using MsgAction = Comet.Game.Packets.MsgAction;

namespace Comet.Game.World.Maps
{
    public sealed class GameMap
    {
        public const uint DEFAULT_LIGHT_RGB = 0xFFFFFF;

        private readonly DbMap m_dbMap;
        private readonly DbDynamap m_dbDynamap;
        private GameMapData mMapData;

        private GameBlock[,] m_blocks;

        private readonly ConcurrentDictionary<uint, Character> m_users = new();
        private readonly ConcurrentDictionary<uint, Role> m_roles = new();

        private readonly List<Passway> m_passway = new();
        private List<DbRegion> m_regions = new();

        public Weather Weather;

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

        public byte ResLev
        {
            get => m_dbMap?.ResourceLevel ?? m_dbDynamap?.ResourceLevel ?? 0;
            set
            {
                if (m_dbMap != null)
                    m_dbMap.ResourceLevel = value;
                else if (m_dbDynamap != null)
                    m_dbDynamap.ResourceLevel = value;
            }
        }

        public int Width => mMapData?.Width ?? 0;
        public int Height => mMapData?.Height ?? 0;

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

        public async Task<bool> InitializeAsync()
        {
            if (m_dbMap == null && m_dbDynamap == null) return false;

            mMapData = MapDataManager.GetMapData(MapDoc);
            if (mMapData == null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"Could not load map {Identity}({MapDoc}): map data not found");
                return false;
            }

            Weather = new Weather(this);

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
                    await Log.WriteLogAsync(LogLevel.Error, $"Could not find portal for passway [{dbPassway.Identity}]");
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

            if (IsSynMap() || IsFamilyMap() || IsPkField())
            {
                Partition = ServerProcessor.PVP_MAP_GROUP;
            }
            else
            {
                Partition = (int)Kernel.Services.Processor.SelectPartition();
            }

            return true;
        }

        public async Task LoadTrapsAsync()
        {
            foreach (DbTrap dbTrap in (await TrapRepository.GetAsync()).Where(x => x.MapId == Identity))
            {
                var trap = new MapTrap(dbTrap);
                if (!await trap.InitializeAsync())
                {
                    await Log.WriteLogAsync(LogLevel.Error,
                                            $"Could not start system map trap for {Identity} > Trap {dbTrap.Id}");
                }
            }
        }

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

        public DynamicNpc QueryStatuary(Role sender, uint lookface, uint task)
        {
            return Query9BlocksByPos(sender.MapX, sender.MapY)
                   .Where(x => x is DynamicNpc)
                   .Cast<DynamicNpc>()
                   .FirstOrDefault(x => x.Task0 == task && x.Mesh - x.Mesh % 10 == lookface - lookface % 10);
        }

        public List<Character> QueryPlayers(Func<Character, bool> pred)
        {
            return m_users.Values.Where(pred).ToList();
        }

        public List<Role> QueryRoles()
        {
            return m_roles.Values.ToList();
        }

        public List<Role> QueryRoles(Func<Role, bool> pred)
        {
            return m_roles.Values.Where(pred).ToList();
        }

        #endregion

        #region Role Management

        public async Task<bool> AddAsync(Role role)
        {
            if (m_roles.TryAdd(role.Identity, role))
            {
                EnterBlock(role, role.MapX, role.MapY);

                if (role is Character character)
                {
                    m_users.TryAdd(character.Identity, character);
                    await character.Screen.UpdateAsync();
                }
                else
                {
                    RoleManager.AddRole(role);

                    if (role.Screen != null)
                        await role.Screen.UpdateAsync();
                    else
                        foreach (Character user in m_users.Values.Where(x =>
                                                                            Calculations.GetDistance(x.MapX, x.MapY, role.MapX, role.MapY) <= Screen.BROADCAST_SIZE))
                            await user.Screen.SpawnAsync(role);
                }

                return true;
            }

            return false;
        }

        public async Task<bool> RemoveAsync(uint idRole)
        {
            if (m_roles.TryRemove(idRole, out Role role))
            {
                m_users.TryRemove(idRole, out _);
                LeaveBlock(role);

                if (!(role is Character))
                    RoleManager.RemoveRole(idRole);

                foreach (Character user in m_users.Values)
                {
                    if (Calculations.GetDistance(role.MapX, role.MapY, user.MapX, user.MapY) > Screen.BROADCAST_SIZE)
                        continue;

                    await user.Screen.RemoveAsync(idRole, true);
                }
            }

            return false;
        }

        #endregion

        #region Broadcasting

        public async Task SendMapInfoAsync(Character user)
        {
            var action = new MsgAction
            {
                Action = MsgAction<Client>.ActionType.MapArgb,
                Identity = 1,
                Command = Light,
                Argument = 0
            };
            await user.SendAsync(action);
            await user.SendAsync(new MsgMapInfo(Identity, MapDoc, Type));

            if (Weather.GetType() != Weather.WeatherType.WeatherNone)
                await Weather.SendWeatherAsync(user);
            else
                await Weather.SendNoWeatherAsync(user);
        }

        public async Task BroadcastMsgAsync(IPacket msg, uint exclude = 0)
        {
            foreach (Character user in m_users.Values)
            {
                if (user.Identity == exclude)
                    continue;

                await user.SendAsync(msg);
            }
        }

        public async Task BroadcastMsgAsync(string message, TalkChannel channel = TalkChannel.TopLeft,
                                            Color? color = null)
        {
            foreach (Character user in m_users.Values) await user.SendAsync(message, channel, color);
        }

        public async Task BroadcastRoomMsgAsync(int x, int y, IPacket msg, uint exclude = 0)
        {
            foreach (Character user in m_users.Values)
            {
                if (user.Identity == exclude ||
                    Calculations.GetDistance(x, y, user.MapX, user.MapY) > Screen.BROADCAST_SIZE)
                    continue;

                await user.SendAsync(msg);
            }
        }

        public async Task BroadcastRoomMsgAsync(IPacket msg, int x, int y, uint exclude, int distance = Screen.VIEW_SIZE * 2)
        {
            foreach (Character user in m_users.Values)
            {
                if (user.Identity == exclude || Calculations.GetDistance(x, y, user.MapX, user.MapY) > distance)
                    continue;

                await user.SendAsync(msg);
            }
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

        public async Task<Point> QueryRandomPositionAsync(int sourceX = 0, int sourceY = 0, int radius = 0)
        {
            for (int i = 0; i < 20; i++)
            {
                int w = radius > 0 ? radius : Width;
                int h = radius > 0 ? radius : Height;

                int randX = await Kernel.NextAsync(w);
                int randY = await Kernel.NextAsync(h);

                int x = sourceX + randX;
                int y = sourceY + randY;

                if (IsStandEnable(x, y))
                    return new Point(x, y);
            }
            return default;
        }

        /// <summary>
        ///     Determinate if a coordinate has already been occupied by another object.
        /// </summary>
        /// <returns>False if the block is empty.</returns>
        public bool IsSuperPosition(int x, int y)
        {
            return GetBlock(GetBlockX(x), GetBlockY(y))?.RoleSet.Values
                                                       .Any(a => a.MapX == x && a.MapY == y && a.IsAlive) != false;
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
            return mMapData.GetFloorMask(x, y) == 0;
        }

        public bool IsMoveEnable(int x, int y)
        {
            if (!IsValidPoint(x, y))
                return false;
            if (mMapData.GetFloorMask(x, y) != 0)
                return false;
            return true;
        }

        public bool IsAltEnable(int sX, int sY, int x, int y, int altDiff = 26)
        {
            if (!IsValidPoint(x, y))
                return false;

            if (Math.Abs(mMapData.GetFloorAlt(x, y) - mMapData.GetFloorAlt(sX, sY)) >= altDiff)
                return false;

            return true;
        }

        public bool IsAltOver(int x, int y, int alt)
        {
            if (!IsValidPoint(x, y))
                return false;
            if (mMapData.GetFloorAlt(x, y) > alt)
                return true;
            return false;
        }

        public bool IsLayItemEnable(int x, int y)
        {
            return IsStandEnable(x, y) && IsMoveEnable(x, y) && m_roles.Values.All(
                       role => role is not MapItem && role is not BaseNpc || role.MapX != x || role.MapY != y);
        }

        public bool FindDropItemCell(int range, ref Point sender)
        {
            if (IsLayItemEnable(sender.X, sender.Y))
                return true;

            int size = range * 2 + 1;
            int bufSize = size ^ 2;

            for (var i = 0; i < 8; i++)
            {
                int newX = sender.X + GameMapData.WalkXCoords[i];
                int newY = sender.Y + GameMapData.WalkYCoords[i];
                if (IsLayItemEnable(newX, newY))
                {
                    sender.X = newX;
                    sender.Y = newY;
                    return true;
                }
            }

            Point pos = sender;
            List<MapItem> setItem = Query9BlocksByPos(sender.X, sender.Y)
                                    .Where(x => x is MapItem && x.GetDistance(pos.X, pos.Y) <= range).Cast<MapItem>()
                                    .ToList();
            int nMinRange = range + 1;
            var ret = false;
            var posFree = new Point();
            for (int i = Math.Max(sender.X - range, 0); i <= sender.X + range && i < Width; i++)
            for (int j = Math.Max(sender.Y - range, 0); j <= sender.Y + range && j < Height; j++)
            {
                int idx = GameMapData.Pos2Index(i - (sender.X - range), j - (sender.Y - range), size, size);

                if (idx >= 0 && idx < bufSize)
                    if (setItem.FirstOrDefault(
                            x => GameMapData.Pos2Index(x.MapX - i + range, x.MapY - j + range, range, range) == idx) !=
                        null)
                        continue;

                if (IsLayItemEnable(sender.X, sender.Y))
                {
                    double nDistance = Calculations.GetDistance(i, j, sender.X, sender.Y);
                    if (nDistance < nMinRange)
                    {
                        nMinRange = (int) nDistance;
                        posFree.X = i;
                        posFree.Y = j;
                        ret = true;
                    }
                }
            }

            if (ret)
            {
                sender = posFree;
                return true;
            }

            return false;
        }

        #endregion

        #region Portals and Passages

        public bool GetRebornMap(ref uint idMap, ref Point target)
        {
            idMap = m_dbMap?.RebornMap ?? m_dbDynamap.RebornMap;
            GameMap targetMap = MapManager.GetMap(idMap);
            if (targetMap == null)
            {
                Log.WriteLogAsync(LogLevel.Error, $"Could not get reborn map [{Identity}]!").ConfigureAwait(false);
                return false;
            }

            if (m_dbMap.LinkX == 0 || m_dbMap.LinkY == 0)
                target = new Point(targetMap.PortalX, targetMap.PortalY);
            else
                target = new Point(m_dbMap.LinkX, m_dbMap.LinkY);

            return true;
        }

        public bool GetPassageMap(ref uint idMap, ref Point target, ref Point source)
        {
            if (!IsValidPoint(source.X, source.Y))
                return false;

            int idxPassage = mMapData.GetPassage(source.X, source.Y);
            if (idxPassage < 0)
                return false;

            if (IsDynamicMap())
            {
                idMap = m_dbDynamap.LinkMap;
                target.X = m_dbDynamap.LinkX;
                target.Y = m_dbDynamap.LinkY;
                return true;
            }

            Passway passway = m_passway.FirstOrDefault(x => x.Index == idxPassage);
            idMap = passway.TargetMap;
            target = new Point(passway.TargetX, passway.TargetY);
            return true;
        }

        #endregion

        #region Regions

        public bool QueryRegion(RegionTypes regionType, ushort x, ushort y)
        {
            return m_regions
                   .Where(re => x > re.BoundX && x < re.BoundX + re.BoundCX && y > re.BoundY &&
                                y < re.BoundY + re.BoundCY).Any(region => region.Type == (int) regionType);
        }

        #endregion

        #region Terrain

        public int GetFloorAlt(int x, int y)
        {
            return mMapData.GetFloorAlt(x, y);
        }

        public async Task<bool> AddTerrainObjectAsync(uint owner, int x, int y, uint idTerrainType)
        {
            if (mMapData.AddTerrainItem(owner, x, y, idTerrainType))
            {
                await Kernel.BroadcastWorldMsgAsync(new MsgAiAction
                {
                    Action = MsgAiAction<AiClient>.AiAction.AddTerrainObj,
                    Data = (int) Identity,
                    Command = (int) idTerrainType,
                    Param = (int) owner,
                    X = (ushort) x,
                    Y = (ushort) y
                });
                return true;
            }

            return false;
        }

        public async Task<bool> DelTerrainObjAsync(uint idOwner)
        {
            if (mMapData.DelTerrainItem(idOwner))
            {
                await Kernel.BroadcastWorldMsgAsync(new MsgAiAction
                {
                    Action = MsgAiAction<AiClient>.AiAction.DelTerrainObj,
                    Data = (int) Identity,
                    Param = (int) idOwner
                });
                return true;
            }

            return false;
        }

        #endregion

        #region Status

        public async Task SetStatusAsync(ulong flag, bool add)
        {
            ulong oldFlag = Flag;
            if (add)
                Flag |= flag;
            else
                Flag &= ~flag;

            if (Flag != oldFlag)
                await BroadcastMsgAsync(new MsgMapInfo(Identity, MapDoc, Flag));
        }

        public void ResetBattle()
        {
            foreach (Character player in m_users.Values)
            {
                //player.BattleSystem.ResetBattle();
            }
        }

        #endregion

        #region Tiles

        public Tile this[int x, int y] => mMapData[x, y];

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

        #region Socket

        public Task SendAddToNpcServerAsync()
        {
            if (m_dbDynamap != null)
                return Kernel.BroadcastWorldMsgAsync(new MsgAiDynaMap(m_dbDynamap));
            return Task.CompletedTask;
        }

        public Task SendRemoveToNpcServerAsync()
        {
            if (m_dbDynamap != null)
                return Kernel.BroadcastWorldMsgAsync(new MsgAiDynaMap(Identity));
            return Task.CompletedTask;
        }

        #endregion

        #region Database

        public async Task<bool> SaveAsync()
        {
            if (m_dbMap == null && m_dbDynamap == null)
                return false;

            if (m_dbMap != null)
                return await ServerDbContext.SaveAsync(m_dbMap);
            return await ServerDbContext.SaveAsync(m_dbDynamap);
        }

        public async Task<bool> DeleteAsync()
        {
            if (m_dbMap == null && m_dbDynamap == null)
                return false;

            if (m_dbMap != null)
                return await ServerDbContext.DeleteAsync(m_dbMap);
            return await ServerDbContext.DeleteAsync(m_dbDynamap);
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
}