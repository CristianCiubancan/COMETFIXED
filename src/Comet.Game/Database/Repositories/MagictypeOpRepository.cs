using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class MagictypeOpRepository
    {
        public static async Task<List<DbMagictypeOp>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.MagictypeOps.ToList();
        }
    }
}