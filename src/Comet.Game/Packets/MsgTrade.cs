using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgTrade : MsgTrade<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = null;

            switch (Action)
            {
                case TradeAction.Apply:
                {
                    if (Data == 0)
                        return;

                    target = RoleManager.GetUser(Data);
                    if (target == null || target.MapIdentity != user.MapIdentity ||
                        target.GetDistance(user) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTargetNotInRange);
                        return;
                    }

                    if (user.Trade != null)
                    {
                        await user.SendAsync(Language.StrTradeYouAlreadyTrade);
                        return;
                    }

                    if (target.Trade != null)
                    {
                        await user.SendAsync(Language.StrTradeTargetAlreadyTrade);
                        return;
                    }

                    if (target.QueryRequest(RequestType.Trade) == user.Identity)
                    {
                        target.PopRequest(RequestType.Trade);
                        user.Trade = target.Trade = new Trade(target, user);
                        await user.SendAsync(new MsgTrade {Action = TradeAction.Open, Data = target.Identity});
                        await target.SendAsync(new MsgTrade {Action = TradeAction.Open, Data = user.Identity});
                        return;
                    }

                    Data = user.Identity;
                    await target.SendAsync(this);
                    await target.SendRelationAsync(user);
                    user.SetRequest(RequestType.Trade, target.Identity);
                    await user.SendAsync(Language.StrTradeRequestSent);
                    break;
                }

                case TradeAction.Quit:
                {
                    if (user.Trade != null)
                        await user.Trade.SendCloseAsync();
                    break;
                }

                case TradeAction.AddItem:
                {
                    if (user.Trade != null)
                        await user.Trade.AddItemAsync(Data, user);
                    break;
                }

                case TradeAction.AddMoney:
                {
                    if (user.Trade != null)
                        await user.Trade.AddMoneyAsync(Data, user);
                    break;
                }

                case TradeAction.Accept:
                {
                    if (user.Trade != null)
                        await user.Trade.AcceptAsync(user.Identity);
                    break;
                }

                case TradeAction.AddConquerPoints:
                {
                    if (user.Trade != null)
                        await user.Trade.AddEmoneyAsync(Data, user);
                    break;
                }
            }
        }
    }
}