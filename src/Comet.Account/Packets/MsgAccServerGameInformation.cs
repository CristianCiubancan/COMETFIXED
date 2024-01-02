using System;
using System.Threading.Tasks;
using Comet.Account.Database;
using Comet.Account.States;
using Comet.Database.Entities;
using Comet.Network.Packets.Internal;

namespace Comet.Account.Packets
{
    public sealed class MsgAccServerGameInformation : MsgAccServerGameInformation<GameServer>
    {
        public override async Task ProcessAsync(GameServer client)
        {
            await ServerDbContext.SaveAsync(new DbRealmStatus
            {
                RealmIdentity = client.Realm.RealmID,
                RealmName = client.Realm.Name,
                NewStatus = DbRealm.RealmStatus.Online,
                OldStatus = DbRealm.RealmStatus.Online,
                MaxPlayersOnline = (uint) PlayerCountRecord,
                PlayersOnline = (uint) PlayerCount,
                Time = DateTime.Now
            });
        }
    }
}