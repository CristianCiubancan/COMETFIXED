using System.Collections.Generic;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Syndicates;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgFactionRankInfo : MsgFactionRankInfo<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            Syndicate syn = user?.Syndicate;
            if (syn == null)
                return;

            List<SyndicateMember> members = syn.QueryRank(DonationType);
            for (var i = 0; i < MAX_COUNT && i < members.Count; i++)
            {
                SyndicateMember member = members[i];
                Members.Add(new MemberListInfoStruct
                {
                    PlayerIdentity = member.UserIdentity,
                    PlayerName = member.UserName,
                    Silvers = member.Silvers / 10000,
                    ConquerPoints = member.ConquerPointsDonation * 20,
                    GuideDonation = member.GuideDonation,
                    PkDonation = member.PkDonation,
                    ArsenalDonation = member.ArsenalDonation,
                    RedRose = member.RedRoseDonation,
                    WhiteRose = member.WhiteRoseDonation,
                    Orchid = member.OrchidDonation,
                    Tulip = member.TulipDonation,
                    TotalDonation = (uint) member.TotalDonation,
                    UsableDonation = member.UsableDonation,
                    Position = i,
                    Rank = (int) member.Rank
                });
            }

            await client.SendAsync(this);
        }
    }
}