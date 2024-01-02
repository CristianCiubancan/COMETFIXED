using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class ItemtypeRepository
    {
        public static async Task<List<DbItemtype>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.Itemtypes.ToList();
        }
    }
}