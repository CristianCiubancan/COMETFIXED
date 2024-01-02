using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Internal.AI;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.Game.Packets.Ai
{
    public sealed class MsgAiSpawnNpc : MsgAiSpawnNpc<AiClient>
    {
        public override async Task ProcessAsync(AiClient client)
        {
            switch (Mode)
            {
                case AiSpawnNpcMode.Spawn:
                {
                    var msg = new MsgAiSpawnNpc
                    {
                        Mode = AiSpawnNpcMode.DestroyNpc
                    };
                    // Spawning ai npc to the world! npc server manages the monsters ids, probably no need to check
                    foreach (SpawnNpc npc in List)
                    {
                        GameMap map = MapManager.GetMap(npc.MapId);
                        if (map == null)
                        {
                            // send result back?
                            msg.List.Add(npc);
                            continue;
                        }

                        DbMonstertype dbMonstertype = RoleManager.GetMonstertype(npc.MonsterType);
                        if (dbMonstertype == null)
                        {
                            // send result back?
                            msg.List.Add(npc);
                            continue;
                        }

                        var monster = new Monster(dbMonstertype, npc.Id, npc.GeneratorId, npc.OwnerId);
                        if (!await monster.InitializeAsync(npc.MapId, npc.X, npc.Y))
                        {
                            // send result back?
                            msg.List.Add(npc);
                            continue;
                        }

                        // Queue the map interaction
                        monster.QueueAction(async () => await monster.EnterMapAsync());
                    }

                    break;
                }

                case AiSpawnNpcMode.DestroyNpc:
                {
                    foreach (SpawnNpc npc in List)
                    {
                        Role role = RoleManager.GetRole(npc.Id);
                        if (role == null || role is not Monster)
                            continue;

                        role.QueueAction(async () => await role.LeaveMapAsync());
                    }

                    break;
                }

                case AiSpawnNpcMode.DestroyGenerator:
                {
                    await Log.WriteLogAsync("Invalid call to Destroy Generator in Game Server");
                    break;
                }
            }
        }
    }
}