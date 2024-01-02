using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Syndicates;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgSynpOffer : MsgSynpOffer<Client>
    {
        public MsgSynpOffer()
        {
        }

        public MsgSynpOffer(SyndicateMember member)
        {
            Identity = 0;
            Silver = member.Silvers / 10000;
            ConquerPoints = member.ConquerPointsDonation * 20;
            GuideDonation = member.GuideDonation;
            PkDonation = member.PkDonation;
            ArsenalDonation = member.ArsenalDonation;
            RedRoseDonation = member.RedRoseDonation;
            WhiteRoseDonation = member.WhiteRoseDonation;
            OrchidDonation = member.OrchidDonation;
            TulipDonation = member.TulipDonation;
            SilverTotal = (uint) (member.SilversTotal / 10000);
            ConquerPointsTotal = member.ConquerPointsTotalDonation * 20;
            GuideTotal = member.GuideDonation;
            PkTotal = member.PkTotalDonation;
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            if (user == null)
                return;

            if (user.SyndicateIdentity > 0)
                await user.SendAsync(new MsgSynpOffer(user.SyndicateMember));
        }
    }
}