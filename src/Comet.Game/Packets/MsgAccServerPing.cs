using System.Threading.Tasks;
using Comet.Game.Internal.Auth;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Internal;

namespace Comet.Game.Packets
{
    public sealed class MsgAccServerPing : MsgAccServerPing<AccountServer>
    {
        public override Task ProcessAsync(AccountServer client)
        {
            return client.SendAsync(new MsgAccServerGameInformation
            {
                PlayerCount = RoleManager.OnlinePlayers,
                PlayerCountRecord = RoleManager.MaxOnlinePlayers,
                PlayerLimit = RoleManager.MaxOnlinePlayers,
                Status = 1
            });
        }
    }
}