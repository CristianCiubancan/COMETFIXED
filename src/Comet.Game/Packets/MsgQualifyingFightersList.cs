using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgQualifyingFightersList : MsgQualifyingFightersList<Client>
    {
        public override Task ProcessAsync(Client client)
        {
            MsgQualifyingFightersList msg = CreateMsg(Page);
            if (msg == null)
                return Task.CompletedTask;
            return client.SendAsync(msg);
        }

        public static MsgQualifyingFightersList CreateMsg(int page = 0)
        {
            var qualifier = EventManager.GetEvent<ArenaQualifier>();
            List<ArenaQualifier.QualifierMatch> fights = qualifier?.QueryMatches(page * 6, 6);
            if (fights == null)
                return null;

            var msg = new MsgQualifyingFightersList
            {
                Page = page,
                FightersNum = qualifier.PlayersOnQueue
            };

            foreach (ArenaQualifier.QualifierMatch fight in fights)
                msg.Fights.Add(new FightStruct
                {
                    Fighter0 = new FighterInfoStruct
                    {
                        Identity = fight.Player1.Identity,
                        Name = fight.Player1.Name,
                        Rank = fight.Player1.QualifierRank,
                        Level = fight.Player1.Level,
                        Profession = fight.Player1.Profession,
                        Points = (int) fight.Player1.QualifierPoints,
                        CurrentHonor = (int) fight.Player1.HonorPoints,
                        LossToday = (int) fight.Player1.QualifierDayLoses,
                        WinsToday = (int) fight.Player1.QualifierDayWins,
                        Mesh = fight.Player1.Mesh,
                        TotalHonor = (int) fight.Player1.HistoryHonorPoints
                    },
                    Fighter1 = new FighterInfoStruct
                    {
                        Identity = fight.Player2.Identity,
                        Name = fight.Player2.Name,
                        Rank = fight.Player2.QualifierRank,
                        Level = fight.Player2.Level,
                        Profession = fight.Player2.Profession,
                        Points = (int) fight.Player2.QualifierPoints,
                        CurrentHonor = (int) fight.Player2.HonorPoints,
                        LossToday = (int) fight.Player2.QualifierDayLoses,
                        WinsToday = (int) fight.Player2.QualifierDayWins,
                        Mesh = fight.Player2.Mesh,
                        TotalHonor = (int) fight.Player2.HistoryHonorPoints
                    }
                });

            return msg;
        }
    }
}