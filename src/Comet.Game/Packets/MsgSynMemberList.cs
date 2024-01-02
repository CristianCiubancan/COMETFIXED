using System.Threading.Tasks;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgSynMemberList : MsgSynMemberList<Client>
    {
        public override Task ProcessAsync(Client client)
        {
            if (client.Character?.Syndicate == null)
                return Task.CompletedTask;

            return client.Character.Syndicate.SendMembersAsync(Index, client.Character);
        }
    }
}