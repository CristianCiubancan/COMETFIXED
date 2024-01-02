using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;
using static Comet.Game.World.Managers.FlowerManager;

namespace Comet.Game.Packets
{
    public sealed class MsgSuitStatus : MsgSuitStatus<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (user.Gender != 2 || user.Transformation != null)
                return;

            Param = (int) user.Identity;
            if (Action == 2)
            {
                user.FairyType = 0;
                await user.BroadcastRoomMsgAsync(this, true);
                return;
            }
            
            List<FlowerRankingStruct> ranking;
            List<FlowerRankingStruct> rankingToday;

            switch (Data) // validate :]
            {
                case 1000: // RedRose
                {
                    ranking = GetFlowerRanking(MsgFlower<Client>.FlowerType.RedRose, 0, 100);
                    rankingToday = GetFlowerRankingToday(MsgFlower<Client>.FlowerType.RedRose, 0, 100);
                    break;
                }

                case 1002: // Orchids
                {
                    ranking = GetFlowerRanking(MsgFlower<Client>.FlowerType.Orchid, 0, 100);
                    rankingToday = GetFlowerRankingToday(MsgFlower<Client>.FlowerType.Orchid, 0, 100);
                    break;
                }

                case 1003: // Tulips
                {
                    ranking = GetFlowerRanking(MsgFlower<Client>.FlowerType.Tulip, 0, 100);
                    rankingToday = GetFlowerRankingToday(MsgFlower<Client>.FlowerType.Tulip, 0, 100);
                    break;
                }

                case 1001: // Lily
                {
                    ranking = GetFlowerRanking(MsgFlower<Client>.FlowerType.WhiteRose, 0, 100);
                    rankingToday = GetFlowerRankingToday(MsgFlower<Client>.FlowerType.WhiteRose, 0, 100);
                    break;
                }

                default:
                {
                    return;
                }
            }

            int myRank = ranking.FirstOrDefault(x => x.Identity == user.Identity).Position;
            int myRankToday = rankingToday.FirstOrDefault(x => x.Identity == user.Identity).Position;

            if ((myRank <= 0 || myRank > 100) && (myRankToday <= 0 || myRankToday > 100))
                return; // not in top 100

            // let's limit the amount of fairies (per type)
            int fairyCount = RoleManager.QueryUserSet().Count(x => x.FairyType == Data);
            if (fairyCount >= 3)
                // message? na
                return;

            if (user.FairyType != 0)
                await user.BroadcastRoomMsgAsync(new MsgSuitStatus
                {
                    Action = 2,
                    Data = (int) user.FairyType,
                    Param = Param
                }, true);

            user.FairyType = (uint) Data;
            await user.BroadcastRoomMsgAsync(this, true);
        }
    }
}