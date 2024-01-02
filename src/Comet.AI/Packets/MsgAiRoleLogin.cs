using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World;
using Comet.AI.World.Managers;
using Comet.AI.World.Maps;
using Comet.Database.Entities;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.AI.Packets
{
    public sealed class MsgAiRoleLogin : MsgAiRoleLogin<Server>
    {
        /// <inheritdoc />
        public override async Task ProcessAsync(Server client)
        {
            switch (NpcType)
            {
                case RoleLoginNpcType.Monster:
                {
                    // must not use
                    break;
                }

                case RoleLoginNpcType.CallPet:
                {
                    DbMonstertype monsterType = RoleManager.GetMonstertype((uint) LookFace);
                    if (monsterType == null)
                    {
                        await Log.WriteLogAsync(LogLevel.Warning,
                                                $"Could not create monster for type {LookFace}");
                        return;
                    }

                    GameMap map = MapManager.GetMap(MapId);
                    if (map == null)
                    {
                        await Log.WriteLogAsync(LogLevel.Warning,
                                                $"Could not create monster for map {MapId}");
                        return;
                    }

                    Monster pet = new Monster(monsterType, Identity, new Generator(MapId, (uint)LookFace, MapX, MapY, 1, 1));
                    if (!await pet.InitializeAsync(MapId, MapX, MapY))
                    {
                        return;
                    }
                    await pet.EnterMapAsync(false);
                    RoleManager.AddRole(pet);
                    break;
                }
            }
        }
    }
}
