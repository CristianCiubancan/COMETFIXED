using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World;
using Comet.AI.World.Managers;
using Comet.AI.World.Maps;
using Comet.Database.Entities;
using Comet.Network.Packets.Ai;
using Comet.Shared;
using Newtonsoft.Json;

namespace Comet.AI.Packets
{
    public sealed class MsgAiSpawnNpc : MsgAiSpawnNpc<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            switch (Mode)
            {
                case AiSpawnNpcMode.Spawn:
                {
                    // Probably a Guard or something spawned by an action (Must have an existing generator)
                    foreach (SpawnNpc target in List)
                    {
                        DbMonstertype monstertype = RoleManager.GetMonstertype(target.MonsterType);
                        if (monstertype == null)
                        {
                            await Log.WriteLogAsync(LogLevel.Warning,
                                                    $"Could not create monster for type {target.MonsterType}");
                            continue;
                        }

                        GameMap map = MapManager.GetMap(target.MapId);
                        if (map == null)
                        {
                            await Log.WriteLogAsync(LogLevel.Warning,
                                                    $"Could not create monster for map {target.MapId}");
                            continue;
                        }

                        Generator generator = GeneratorManager.GetGenerator(target.GeneratorId);
                        if (generator == null)
                        {
                            await Log.WriteLogAsync(LogLevel.Warning,
                                                    $"Could not create monster for generator (no gen) {target.GeneratorId}");
                            continue;
                        }

                        var monster = new Monster(monstertype, (uint) IdentityGenerator.Monster.GetNextIdentity,
                                                  generator);
                        if (!await monster.InitializeAsync(target.MapId, target.X, target.Y))
                        {
                            await Log.WriteLogAsync(LogLevel.Warning,$"ExecuteActionEventCreatepet could not initialize monster: {JsonConvert.SerializeObject(target)}");
                            IdentityGenerator.Monster.ReturnIdentity(monster.Identity);
                            continue;
                        }

                        RoleManager.AddRole(monster);
                        generator.Add(monster);
                        await monster.EnterMapAsync();
                    }

                    break;
                }

                case AiSpawnNpcMode.DestroyNpc:
                {
                    // Seeks and remove an NPC. Request by Game Server, does not need to reply. Already remove server side
                    foreach (SpawnNpc starget in List)
                    {
                        Role target = RoleManager.GetRole(starget.Id);
                        if (target == null)
                            continue;

                        await target.LeaveMapAsync();
                        RoleManager.RemoveRole(target.Identity);
                    }

                    break;
                }

                case AiSpawnNpcMode.DestroyGenerator:
                {
                    // Seeks and destroy an generator. Does not remove the generator from the pool only clears the monsters
                    break;
                }
            }
        }
    }
}