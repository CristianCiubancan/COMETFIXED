using System;
using Comet.Game.Internal.AI;
using Comet.Game.States;
using Comet.Network.Packets.Ai;

namespace Comet.Game.Packets.Ai
{
    public sealed class MsgAiPlayerLogin : MsgAiPlayerLogin<AiClient>
    {
        public MsgAiPlayerLogin(Character user)
        {
            Id = user.Identity;
            Name = user.Name;
            BattlePower = user.BattlePower;
            Level = user.Level;
            Metempsychosis = user.Metempsychosis;
            Flag1 = user.StatusFlag;
            Flag2 = 0;
            Flag3 = 0;
            Life = (int)Math.Max(1, user.Life);
            MaxLife = (int)user.MaxLife;
            Money = (int)user.Silvers;
            ConquerPoints = (int)user.ConquerPoints;
            Syndicate = user.SyndicateIdentity;
            SyndicatePosition = (int)user.SyndicateRank;
            Family = (int)user.FamilyIdentity;
            FamilyPosition = (int)user.FamilyPosition;
            MapId = user.MapIdentity;
            X = user.MapX;
            Y = user.MapY;
            Nobility = (int) user.NobilityRank;
        }
    }
}