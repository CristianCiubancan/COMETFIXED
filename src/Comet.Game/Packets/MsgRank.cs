using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgRank : MsgRank<Client>
    {
        private const int RedRose = 0x1c9c382;
        private const int WhiteRose = 0x1c9c3e6;
        private const int Orchid = 0x1c9c44a;
        private const int Tulip = 0x1c9c4ae;

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            switch (Mode)
            {
                case RequestType.RequestRank:
                {
                    switch (RankMode)
                    {
                        case RankType.Flower:
                        {
                            await QueryFlowerRankingAsync(user, (int) Identity, PageNumber);
                            break;
                        }
                    }

                    break;
                }

                case RequestType.QueryInfo:
                {
                    if (user.Gender != 2)
                        return;

                    FlowerManager.FlowerRankObject flowerToday = await FlowerManager.QueryFlowersAsync(user);
                    await user.SendAsync(new MsgFlower
                    {
                        Mode = MsgFlower<Client>.RequestMode.QueryIcon,
                        Identity = user.Identity,
                        RedRoses = user.FlowerRed,
                        RedRosesToday = flowerToday?.RedRoseToday ?? 0,
                        WhiteRoses = user.FlowerWhite,
                        WhiteRosesToday = flowerToday?.WhiteRoseToday ?? 0,
                        Orchids = user.FlowerOrchid,
                        OrchidsToday = flowerToday?.OrchidsToday ?? 0,
                        Tulips = user.FlowerTulip,
                        TulipsToday = flowerToday?.TulipsToday ?? 0
                    });

                    await user.SendAsync(new MsgRank
                    {
                        Mode = RequestType.QueryIcon,
                        Infos = new List<QueryStruct>()
                    });

                    //if (user.CanRefreshFlowerRank)
                    {
                        List<FlowerManager.FlowerRankingStruct> roseRank =
                            FlowerManager.GetFlowerRanking(MsgFlower<Client>.FlowerType.RedRose, 0, 100);
                        List<FlowerManager.FlowerRankingStruct> lilyRank =
                            FlowerManager.GetFlowerRanking(MsgFlower<Client>.FlowerType.WhiteRose, 0, 100);
                        List<FlowerManager.FlowerRankingStruct> orchidRank =
                            FlowerManager.GetFlowerRanking(MsgFlower<Client>.FlowerType.Orchid, 0, 100);
                        List<FlowerManager.FlowerRankingStruct> tulipRank =
                            FlowerManager.GetFlowerRanking(MsgFlower<Client>.FlowerType.Tulip, 0, 100);

                        List<FlowerManager.FlowerRankingStruct> roseRankToday =
                            FlowerManager.GetFlowerRankingToday(MsgFlower<Client>.FlowerType.RedRose, 0, 100);
                        List<FlowerManager.FlowerRankingStruct> lilyRankToday =
                            FlowerManager.GetFlowerRankingToday(MsgFlower<Client>.FlowerType.WhiteRose, 0, 100);
                        List<FlowerManager.FlowerRankingStruct> orchidRankToday =
                            FlowerManager.GetFlowerRankingToday(MsgFlower<Client>.FlowerType.Orchid, 0, 100);
                        List<FlowerManager.FlowerRankingStruct> tulipRankToday =
                            FlowerManager.GetFlowerRankingToday(MsgFlower<Client>.FlowerType.Tulip, 0, 100);

                        int myRose = roseRank.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myLily = lilyRank.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myOrchid = orchidRank.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myTulip = tulipRank.FirstOrDefault(x => x.Identity == user.Identity).Position;

                        int myRoseToday = roseRankToday.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myLilyToday = lilyRankToday.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myOrchidToday = orchidRankToday.FirstOrDefault(x => x.Identity == user.Identity).Position;
                        int myTulipToday = tulipRankToday.FirstOrDefault(x => x.Identity == user.Identity).Position;

                        uint rankType = 0;
                        uint amount = 0;
                        var rank = 0;

                        var display = false;
                        if (myRoseToday < myRose && myRoseToday > 0 && myRoseToday <= 100)
                        {
                            rankType = RedRose;
                            amount = flowerToday?.RedRoseToday ?? 0;
                            rank = myRoseToday;
                            display = true;
                        }
                        else if (myRose > 0 && myRose <= 100)
                        {
                            rankType = RedRose;
                            amount = user.FlowerRed;
                            rank = myRose;
                            display = true;
                        }

                        MsgRank msg;
                        if (display)
                        {
                            msg = new MsgRank();
                            msg.Mode = RequestType.QueryInfo;
                            msg.Identity = rankType;
                            msg.Infos.Add(new QueryStruct
                            {
                                Type = 1,
                                Amount = amount,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(msg);
                        }

                        display = false;
                        if (myLilyToday < myLily && myLilyToday > 0 && myLilyToday <= 100)
                        {
                            rankType = WhiteRose;
                            amount = flowerToday?.WhiteRoseToday ?? 0;
                            rank = myLilyToday;
                            display = true;
                        }
                        else if (myLily > 0 && myLily <= 100)
                        {
                            rankType = WhiteRose;
                            amount = user.FlowerWhite;
                            rank = myLily;
                            display = true;
                        }

                        if (display)
                        {
                            msg = new MsgRank();
                            msg.Mode = RequestType.QueryInfo;
                            msg.Identity = rankType;
                            msg.Infos.Add(new QueryStruct
                            {
                                Type = 1,
                                Amount = amount,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(msg);
                        }

                        display = false;
                        if (myOrchidToday < myOrchid && myOrchidToday > 0 && myOrchidToday <= 100)
                        {
                            rankType = Orchid;
                            amount = flowerToday?.OrchidsToday ?? 0;
                            rank = myOrchidToday;
                            display = true;
                        }
                        else if (myOrchid > 0 && myOrchid <= 100)
                        {
                            rankType = Orchid;
                            amount = user.FlowerOrchid;
                            rank = myOrchid;
                            display = true;
                        }

                        if (display)
                        {
                            msg = new MsgRank();
                            msg.Mode = RequestType.QueryInfo;
                            msg.Identity = rankType;
                            msg.Infos.Add(new QueryStruct
                            {
                                Type = 1,
                                Amount = amount,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(msg);
                        }

                        display = false;
                        if (myTulipToday < myTulip && myTulipToday > 0 && myTulipToday <= 100)
                        {
                            rankType = Tulip;
                            amount = flowerToday?.TulipsToday ?? 0;
                            rank = myTulipToday;
                            display = true;
                        }
                        else if (myTulip > 0 && myTulip <= 100)
                        {
                            rankType = Tulip;
                            amount = user.FlowerTulip;
                            rank = myTulip;
                            display = true;
                        }

                        if (display)
                        {
                            msg = new MsgRank();
                            msg.Mode = RequestType.QueryInfo;
                            msg.Identity = rankType;
                            msg.Infos.Add(new QueryStruct
                            {
                                Type = 1,
                                Amount = amount,
                                Identity = user.Identity,
                                Name = user.Name
                            });
                            await user.SendAsync(msg);
                        }

                        if (rankType != user.FlowerCharm)
                        {
                            user.FlowerCharm = rankType;
                            await user.Screen.SynchroScreenAsync();
                        }

                        Mode = RequestType.QueryIcon;
                        Infos.Add(new QueryStruct
                        {
                            Type = 1,
                            Amount = amount,
                            Identity = user.Identity,
                            Name = user.Name
                        });
                        await client.SendAsync(this);
                    }

                    await user.SendAsync(new MsgRank
                    {
                        Mode = RequestType.QueryIcon
                    });

                    break;
                }
                default:
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Unhandled MsgRank:{Mode}");
                    await Log.WriteLogAsync(LogLevel.Debug, PacketDump.Hex(Encode()));
                    return;
                }
            }
        }

        private async Task QueryFlowerRankingAsync(Character user, int flowerIdentity, int page)
        {
            if (user.Gender != 2) // only woman at this version
                return;

            int currentPosition = -1;
            List<FlowerManager.FlowerRankingStruct> ranking;
            switch (flowerIdentity)
            {
                case RedRose: // red rose
                {
                    ranking = FlowerManager.GetFlowerRanking(MsgFlower<Client>.FlowerType.RedRose, 0, 100);
                    break;
                }

                case WhiteRose: // white rose
                {
                    ranking = FlowerManager.GetFlowerRanking(MsgFlower<Client>.FlowerType.WhiteRose, 0, 100);
                    break;
                }

                case Orchid: // orchid
                {
                    ranking = FlowerManager.GetFlowerRanking(MsgFlower<Client>.FlowerType.Orchid, 0, 100);
                    break;
                }

                case Tulip: // tulip
                {
                    ranking = FlowerManager.GetFlowerRanking(MsgFlower<Client>.FlowerType.Tulip, 0, 100);
                    break;
                }

                default:
                    return;
            }

            currentPosition = ranking.FirstOrDefault(x => x.Identity == user.Identity).Position;
            if (currentPosition <= 0)
                return;

            const int maxPerPage = 10;
            int index = page * maxPerPage;
            var count = 0;

            if (index >= ranking.Count)
                return;

            var msg = new MsgRank
            {
                Mode = RequestType.RequestRank
            };
            for (; index < ranking.Count && count < 10; index++, count++)
                msg.Infos.Add(new QueryStruct
                {
                    Type = (ulong) index + 1,
                    Amount = ranking[index].Value,
                    Identity = ranking[index].Identity,
                    Name = ranking[index].Name
                });
            await user.SendAsync(msg);
        }
    }
}