using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgQualifyingRank : MsgQualifyingRank<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            int page = Math.Min(0, PageNumber - 1);
            switch (RankType)
            {
                case QueryRankType.QualifierRank:
                {
                    List<DbArenic> players = await ArenicRepository.GetRankAsync(page * 10);
                    int rank = page * 10;

                    foreach (DbArenic player in players)
                        Players.Add(new PlayerDataStruct
                        {
                            Rank = (ushort) (rank++ + 1),
                            Name = player.User.Name,
                            Type = 0,
                            Level = player.User.Level,
                            Profession = player.User.Profession,
                            Points = player.User.AthletePoint,
                            Unknown = (int) player.User.Identity
                        });
                    RankingNum = await ArenicRepository.GetRankCountAsync();
                    break;
                }
                case QueryRankType.HonorHistory:
                {
                    List<DbCharacter> players = await CharactersRepository.GetHonorRankAsync(page * 10, 10);
                    int rank = page * 10 + 1;
                    foreach (DbCharacter player in players)
                        Players.Add(new PlayerDataStruct
                        {
                            Rank = (ushort) (rank++ + 1),
                            Name = player.Name,
                            Type = 6004,
                            Level = player.Level,
                            Profession = player.Profession,
                            Points = player.AthleteHistoryHonorPoints,
                            Unknown = 0
                        });
                    RankingNum = await CharactersRepository.GetHonorRankCountAsync();
                    break;
                }
            }

            await client.SendAsync(this);
        }
    }
}