using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class StatusRepository
    {
        public static async Task<List<DbStatus>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.Status.Where(x => x.OwnerId == idUser).ToList();
        }
    }
}