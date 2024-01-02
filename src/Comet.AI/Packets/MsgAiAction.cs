using System.Threading.Tasks;
using Comet.AI.States;
using Comet.AI.World.Managers;
using Comet.Network.Packets.Ai;
using Comet.Shared;

namespace Comet.AI.Packets
{
    public sealed class MsgAiAction : MsgAiAction<Server>
    {
        public override async Task ProcessAsync(Server client)
        {
            switch (Action)
            {
                case AiAction.RequestLogin:
                {
                    await client.SendAsync(new MsgAiLoginExchange
                    {
                        UserName = Kernel.Configuration.GameNetwork.Username,
                        Password = Kernel.Configuration.GameNetwork.Password,
                        ServerName = Kernel.Configuration.GameNetwork.ServerName
                    });
                    break;
                }

                case AiAction.FlyMap:
                {
                    Role target = RoleManager.GetRole((uint) Data);

                    if (target?.Map == null)
                        return;

                    target.QueueAction(async () =>
                    {
#if DEBUG
                        await Log.WriteLogAsync(LogLevel.Debug,
                                                $"Target '{target.Name}' FlyMap {target.MapIdentity},{target.MapX},{target.MapY}=>{Param},{X},{Y}");
#endif
                        await target.LeaveMapAsync();
                        target.MapIdentity = (uint) Param;
                        target.MapX = X;
                        target.MapY = Y;
                        await target.EnterMapAsync();
                    });
                    break;
                }

                case AiAction.SetProtection:
                {
                    Role target = RoleManager.GetRole((uint)Data);

                    if (target?.Map == null)
                        return;

                    if (target is Character user)
                    {
                        user.SetProtection();
                    }

                    break;
                }

                case AiAction.ClearProtection:
                {
                    Role target = RoleManager.GetRole((uint)Data);

                    if (target?.Map == null)
                        return;

                    if (target is Character user)
                    {
                        user.ClearProtection();
                    }
                    break;
                }
            }
        }
    }
}