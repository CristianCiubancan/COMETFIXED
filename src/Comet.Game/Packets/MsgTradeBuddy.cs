using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgTradeBuddy : MsgTradeBuddy<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = RoleManager.GetUser(Identity);

            switch (Action)
            {
                case TradeBuddyAction.RequestPartnership:
                {
                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTargetNotInRange);
                        return;
                    }

                    if (user.QueryRequest(RequestType.TradePartner) == target.Identity)
                    {
                        user.PopRequest(RequestType.TradePartner);
                        await user.CreateTradePartnerAsync(target);
                        return;
                    }

                    target.SetRequest(RequestType.TradePartner, user.Identity);
                    Identity = user.Identity;
                    Name = user.Name;
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);
                    break;
                }

                case TradeBuddyAction.RejectRequest:
                {
                    if (target == null)
                        return;

                    Identity = user.Identity;
                    Name = user.Name;
                    IsOnline = true;
                    await target.SendAsync(this);
                    break;
                }

                case TradeBuddyAction.BreakPartnership:
                {
                    await user.DeleteTradePartnerAsync(Identity);
                    break;
                }
            }
        }
    }
}