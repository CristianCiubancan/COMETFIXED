using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.AI.Packets
{
    public sealed class MsgAiPlayerLogin : MsgAiPlayerLogin<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            Character user = RoleManager.GetUser(Id);
            if (user != null)
            {
                await Log.WriteLogAsync(LogLevel.Warning,
                                        $"User [{Id}]{Name} is already signed in. Invalid Call (FlyMap??)");
                return;
            }

            user = new Character();
            if (!await user.InitializeAsync(this))
            {
                await Log.WriteLogAsync(LogLevel.Warning, $"User [{Id}]{Name} could not be initialized!");
                return;
            }

            RoleManager.LoginUser(user);
#if DEBUG
            await Log.WriteLogAsync(LogLevel.Debug, $"User [{Id}]{Name} has signed in.");
#endif
        }
    }
}