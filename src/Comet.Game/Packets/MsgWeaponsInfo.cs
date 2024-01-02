using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Game.States.Syndicates;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgWeaponsInfo : MsgWeaponsInfo<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (user?.Syndicate == null)
                return;

            Syndicate syn = user.Syndicate;

            if (Action == 0)
            {
                if (TotemType == (int) Syndicate.TotemPoleType.None)
                    return;

                await syn.SendTotemsAsync(user, (Syndicate.TotemPoleType) TotemType, Data1);
            }
        }
    }
}