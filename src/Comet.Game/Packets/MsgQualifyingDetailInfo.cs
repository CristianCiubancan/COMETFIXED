using System;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgQualifyingDetailInfo : MsgQualifyingDetailInfo<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user == null)
                return;

            Ranking = user.QualifierRank;
            Status = user.QualifierStatus;
            TodayWins = user.QualifierDayWins;
            TodayLoses = user.QualifierDayLoses;
            TotalWins = user.QualifierHistoryWins;
            TotalLoses = user.QualifierHistoryLoses;
            HistoryHonor = user.HistoryHonorPoints;
            CurrentHonor = user.HonorPoints;
            Points = user.QualifierPoints;
            TriumphToday20 = (byte) Math.Min(20, user.QualifierDayGames);
            TriumphToday9 = (byte) Math.Min(9, user.QualifierDayWins);
            await client.SendAsync(this);
        }
    }
}