using Comet.Core;
using Comet.Game.States;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgSyndicateAttributeInfo : MsgSyndicateAttributeInfo<Client>
    {
        public MsgSyndicateAttributeInfo()
        {
            LeaderName = Language.StrNone;
        }
    }
}