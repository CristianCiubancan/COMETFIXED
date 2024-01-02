using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class FriendRepository
    {
        public static async Task<List<DbFriend>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.Friends.Where(x => x.UserIdentity == idUser).ToList();
        }

        public static async Task<DbFriend> GetAsync(uint user, uint target)
        {
            await using var db = new ServerDbContext();
            return db.Friends.FirstOrDefault(x => x.UserIdentity == user && x.TargetIdentity == target);
        }
    }
}