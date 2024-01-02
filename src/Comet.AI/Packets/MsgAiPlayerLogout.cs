using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.AI.Packets
{
    public sealed class MsgAiPlayerLogout : MsgAiPlayerLogout<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            if (!RoleManager.LogoutUser(Id, out Character user))
                return;

            await user.LeaveMapAsync(false);

#if DEBUG
            await Log.WriteLogAsync(LogLevel.Debug, $"User [{Id}]{user.Name} has signed out.");
#endif
        }
    }
}