using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgPeerage : MsgPeerage<Client>
    {
        public MsgPeerage()
        {
            Data = 0;
        }

        public MsgPeerage(NobilityAction action, ushort maxPerPage, ushort maxPages)
        {
            Action = action;
            DataLow2 = maxPages;
            DataHigh = maxPerPage;
        }

        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Action)
            {
                case NobilityAction.Donate:
                    if (user.Level < 70)
                    {
                        await user.SendAsync(Language.StrPeerageDonateErrBelowLevel);
                        return;
                    }

                    if (Data < 3000000)
                    {
                        await user.SendAsync(Language.StrPeerageDonateErrBelowUnderline);
                        return;
                    }

                    if (Data <= user.Silvers)
                    {
                        if (!await user.SpendMoneyAsync((int) Data, true))
                            return;
                    }
                    else
                    {
                        if (!await user.SpendConquerPointsAsync((int) (Data / 50000), true))
                            return;
                    }

                    await PeerageManager.DonateAsync(user, Data);
                    break;
                case NobilityAction.List:
                    await PeerageManager.SendRankingAsync(user, DataLow1);
                    break;
                case NobilityAction.QueryRemainingSilver:
                    Data = (uint) PeerageManager.GetNextRankSilver((NobilityRank) DataLow1, user.NobilityDonation);
                    Data2 = 60;
                    Data3 = (uint) user.NobilityPosition;
                    await user.SendAsync(this);
                    break;
            }
        }
    }
}