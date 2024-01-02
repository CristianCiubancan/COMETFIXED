using Comet.Database.Entities;

namespace Comet.Account.States
{
    public sealed class Player
    {
        public DbAccount Account;
        public uint AccountIdentity;
        public DbRealm Realm;
    }
}