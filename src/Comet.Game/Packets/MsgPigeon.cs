using System.Linq;
using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgPigeon : MsgPigeon<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            switch (Mode)
            {
                case PigeonMode.Query:
                case PigeonMode.QueryUser:
                {
                    await PigeonManager.SendListAsync(client.Character, Mode, Param);
                    break;
                }
                case PigeonMode.Send:
                {
                    await PigeonManager.PushAsync(client.Character, Strings.FirstOrDefault());
                    await PigeonManager.SendListAsync(client.Character, PigeonMode.Query, 0);
                    break;
                }
                case PigeonMode.SuperUrgent:
                case PigeonMode.Urgent:
                {
                    await PigeonManager.AdditionAsync(client.Character, this);
                    await PigeonManager.SendListAsync(client.Character, PigeonMode.Query, 0);
                    break;
                }
            }
        }
    }
}