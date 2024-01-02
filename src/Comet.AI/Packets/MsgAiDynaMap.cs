using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.AI.World.Maps;
using Comet.Database.Entities;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.AI.Packets
{
    public sealed class MsgAiDynaMap : MsgAiDynaMap<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            if (Mode == 0) // add
            {
                var dynaMap = new DbDynamap
                {
                    Identity = Identity,
                    Name = Name,
                    Description = Description,
                    Type = MapType,
                    LinkMap = LinkMap,
                    LinkX = LinkX,
                    LinkY = LinkY,
                    MapDoc = MapDoc,
                    MapGroup = MapGroup,
                    OwnerIdentity = OwnerIdentity,
                    OwnerType = OwnerType,
                    PortalX = PortalX,
                    PortalY = PortalY,
                    RebornMap = RebornMap,
                    RebornPortal = RebornPortal,
                    ResourceLevel = ResourceLevel,
                    ServerIndex = ServerIndex,
                    Weather = Weather,
                    BackgroundMusic = BackgroundMusic,
                    BackgroundMusicShow = BackgroundMusicShow,
                    Color = Color
                };

                var map = new GameMap(dynaMap);
                if (!await map.InitializeAsync())
                    return;

                MapManager.AddMap(map);

#if DEBUG
                await Log.WriteLogAsync(LogLevel.Debug,
                                        $"Map {map.Identity} {map.Name} {Description} has been added to the pool.");
#endif
            }
            else
            {
                MapManager.RemoveMap(Identity);

#if DEBUG
                await Log.WriteLogAsync(LogLevel.Debug, $"Map {Identity} has been removed from the pool.");
#endif
            }
        }
    }
}