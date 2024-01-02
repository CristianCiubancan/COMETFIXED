using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgQualifyingSeasonRankList : MsgQualifyingSeasonRankList<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            List<DbArenic> rank = await ArenicRepository.GetSeasonRankAsync(DateTime.Now.AddDays(-1));
            ushort pos = 1;
            foreach (DbArenic obj in rank)
                Members.Add(new QualifyingSeasonRankStruct
                {
                    Rank = pos++,
                    Identity = obj.UserId,
                    Name = obj.User.Name,
                    Level = obj.User.Level,
                    Profession = obj.User.Profession,
                    Win = (int) obj.DayWins,
                    Lose = (int) obj.DayLoses,
                    Mesh = obj.User.Mesh,
                    Score = (int) obj.AthletePoint
                });
            await client.SendAsync(this);
        }
    }
}