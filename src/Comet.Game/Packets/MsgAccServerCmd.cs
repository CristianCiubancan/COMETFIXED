using System.Threading.Tasks;
using Comet.Game.Internal.Auth;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Internal;

namespace Comet.Game.Packets
{
    public sealed class MsgAccServerCmd : MsgAccServerCmd<AccountServer>
    {
        public override async Task ProcessAsync(AccountServer client)
        {
            Character account = RoleManager.GetUserByAccount(AccountIdentity);
            if (account == null)
                return;

            switch (Action)
            {
                case ServerAction.Disconnect:
                {
                    await RoleManager.KickOutAsync(account.Identity, "REALM MANAGER REQUEST");
                    break;
                }
                case ServerAction.Ban:
                {
                    await RoleManager.KickOutAsync(account.Identity, "Account banned!");
                    break;
                }
                case ServerAction.Maintenance:
                {
                    // TODO maintenance manager
                    break;
                }
            }
        }
    }
}