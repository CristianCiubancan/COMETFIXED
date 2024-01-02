using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.AI.Database.Repositories
{
    public static class MagictypeRepository
    {
        public static async Task<List<DbMagictype>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.Magictype.ToList();
        }
    }
}