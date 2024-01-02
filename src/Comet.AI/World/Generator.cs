using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.AI.Packets;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.AI.World.Maps;
using Comet.Database.Entities;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.AI.World
{
    public sealed class Generator
    {
        private readonly DbGenerator m_dbGen;
        private readonly DbMonstertype m_dbMonster;
        private readonly Point m_pCenter;
        private readonly GameMap m_pMap;

        public bool CanBeProcessed;
        private readonly ConcurrentDictionary<uint, TimeOut> m_dicAwaitingToRebirth = new();

        private readonly ConcurrentDictionary<uint, Monster> m_dicMonsters = new();

        private readonly TimeOut mMinTime = new(3);

        public Generator(DbGenerator dbGen)
        {
            m_dbGen = dbGen;

            m_pMap = MapManager.GetMap(m_dbGen.Mapid);
            if (m_pMap == null)
            {
                _ = Log.WriteLogAsync(LogLevel.Error,
                                      $"Could not load map ({m_dbGen.Mapid}) for generator ({m_dbGen.Id})")
                       .ConfigureAwait(false);
                return;
            }

            m_dbMonster = RoleManager.GetMonstertype(m_dbGen.Npctype);
            if (m_dbMonster == null)
            {
                _ = Log.WriteLogAsync(LogLevel.Error,
                                      $"Could not load monster ({m_dbGen.Npctype}) for generator ({m_dbGen.Id})")
                       .ConfigureAwait(false);
                return;
            }

            m_pCenter = new Point(m_dbGen.BoundX + m_dbGen.BoundCx / 2, m_dbGen.BoundY + m_dbGen.BoundCy / 2);
            CanBeProcessed = true;
        }

        public Generator(uint idMap, uint idMonster, ushort usX, ushort usY, ushort usCx, ushort usCy)
        {
            m_dbGen = new DbGenerator
            {
                Mapid = idMap,
                BoundX = usX,
                BoundY = usY,
                BoundCx = usCx,
                BoundCy = usCy,
                Npctype = idMonster,
                MaxNpc = 0,
                MaxPerGen = 0,
                Id = m_idGenerator++
            };

            m_pMap = MapManager.GetMap(m_dbGen.Mapid);
            if (m_pMap == null)
            {
                _ = Log.WriteLogAsync(LogLevel.Error,
                                      $"Could not load map ({m_dbGen.Mapid}) for generator ({m_dbGen.Id})")
                       .ConfigureAwait(false);
                return;
            }

            m_dbMonster = RoleManager.GetMonstertype(m_dbGen.Npctype);
            if (m_dbMonster == null)
            {
                _ = Log.WriteLogAsync(LogLevel.Error,
                                      $"Could not load monster ({m_dbGen.Npctype}) for generator ({m_dbGen.Id})")
                       .ConfigureAwait(false);
                return;
            }

            m_pCenter = new Point(m_dbGen.BoundX + m_dbGen.BoundCx / 2, m_dbGen.BoundY + m_dbGen.BoundCy / 2);
        }

        public int Generated => m_dicMonsters.Count;

        public bool IsActive => m_pMap.PlayerCount > 0 || m_dicMonsters.Values.Any(x => !x.IsAlive);

        public uint Identity => m_dbGen.Id;

        public uint RoleType => m_dbGen.Npctype;

        public int RestSeconds => Math.Max(m_dbGen.RestSecs, _MIN_TIME_BETWEEN_GEN);

        public uint MapIdentity => m_dbGen.Mapid;

        public string MonsterName => m_dbMonster.Name;

        public async Task<Point> FindGenPosAsync()
        {
            var result = new Point
            {
                X = m_dbGen.BoundX + await Kernel.Services.Randomness.NextAsync(0, m_dbGen.BoundCx),
                Y = m_dbGen.BoundY + await Kernel.Services.Randomness.NextAsync(0, m_dbGen.BoundCy)
            };

            if (!m_pMap.IsValidPoint(result.X, result.Y) || !m_pMap.IsStandEnable(result.X, result.Y)) return default;

            return result;
        }

        public async Task<Monster> GenerateMonsterAsync()
        {
            var mob = new Monster(m_dbMonster, (uint) IdentityGenerator.Monster.GetNextIdentity, this);

            Point pos = await FindGenPosAsync();
            if (pos == default)
            {
                IdentityGenerator.Monster.ReturnIdentity(mob.Identity);
                return null;
            }

            if (!await mob.InitializeAsync(m_pMap.Identity, (ushort) pos.X, (ushort) pos.Y))
            {
                IdentityGenerator.Monster.ReturnIdentity(mob.Identity);
                return null;
            }

            return mob;
        }

        public Task GenerateAsync()
        {
            if (!IsActive || !mMinTime.ToNextTime())
                return Task.CompletedTask;

            foreach (KeyValuePair<uint, TimeOut> mob in m_dicAwaitingToRebirth.Where(x => x.Value.IsTimeOut()))
            {
                m_dicMonsters.TryRemove(mob.Key, out _);
                m_dicAwaitingToRebirth.TryRemove(mob.Key, out _);
            }

            int generate = Math.Min(m_dbGen.MaxPerGen - Generated, _MAX_PER_GEN);
            if (generate > 0)
                while (generate-- > 0)
                    Kernel.Services.Processor.Queue(m_pMap.Partition, async () =>
                    {
                        Monster monster = await GenerateMonsterAsync();
                        if (monster == null || !m_dicMonsters.TryAdd(monster.Identity, monster))
                            return;
                        await monster.EnterMapAsync();
                    });
            return Task.CompletedTask;
        }

        public bool Add(Monster monster)
        {
            return m_dicMonsters.TryAdd(monster.Identity, monster);
        }

        public void Remove(uint role)
        {
            if (m_dbGen.MaxPerGen == 0)
            {
                m_dicMonsters.TryRemove(role, out _);
                return;
            }

            if (m_dicMonsters.TryGetValue(role, out Monster mob))
            {
                var tm = new TimeOut();
                tm.Startup(RestSeconds);
                m_dicAwaitingToRebirth.TryAdd(role, tm);
            }
        }

        public async Task ClearGeneratorAsync()
        {
            foreach (Monster monster in m_dicMonsters.Values) await monster.LeaveMapAsync();
        }

        public Monster[] GetRoles()
        {
            return m_dicMonsters.Values.ToArray();
        }

        public Point GetCenter()
        {
            return m_pCenter;
        }

        public bool IsTooFar(ushort x, ushort y, int nRange)
        {
            return !(x >= m_dbGen.BoundX - nRange
                     && x < m_dbGen.BoundX + m_dbGen.BoundCx + nRange
                     && y >= m_dbGen.BoundY - nRange
                     && y < m_dbGen.BoundY + m_dbGen.BoundCy + nRange);
        }

        public bool IsInRegion(int x, int y)
        {
            return x >= m_dbGen.BoundX && x < m_dbGen.BoundX + m_dbGen.BoundCx
                                       && y >= m_dbGen.BoundY && y < m_dbGen.BoundY + m_dbGen.BoundCy;
        }

        public int GetWidth()
        {
            return m_dbGen.BoundCx;
        }

        public int GetHeight()
        {
            return m_dbGen.BoundCy;
        }

        public int GetPosX()
        {
            return m_dbGen.BoundX;
        }

        public int GetPosY()
        {
            return m_dbGen.BoundY;
        }

        public async Task<int> SendAllAsync()
        {
            if (Kernel.GameServer == null || !Kernel.GameServer.Socket.Connected)
                return 0;

            var result = 0;
            MsgAiSpawnNpc msg = new();
            msg.Mode = AiSpawnNpcMode.Spawn;
            foreach (Monster npc in m_dicMonsters.Values.Where(x => x.IsAlive))
            {
                if (msg.List.Count >= 25)
                {
                    result += msg.List.Count;
                    await Kernel.SendAsync(msg);
                    msg.List.Clear();
                }

                msg.List.Add(new MsgAiSpawnNpc<Server>.SpawnNpc
                {
                    Id = npc.Identity,
                    GeneratorId = Identity,
                    MonsterType = npc.Type,
                    MapId = npc.MapIdentity,
                    X = npc.MapX,
                    Y = npc.MapY
                });
            }

            if (msg.List.Count > 0)
            {
                result += msg.List.Count;
                await Kernel.SendAsync(msg);
            }

            return result;
        }

        private const int _MAX_PER_GEN = 15;
        private const int _MIN_TIME_BETWEEN_GEN = 10;
        private static uint m_idGenerator = 2000000;
    }
}