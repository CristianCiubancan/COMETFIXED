using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class ItemRepository
    {
        public static async Task<List<DbItem>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.Items.Where(x => x.PlayerId == idUser).ToList();
        }

        public static async Task<DbItem> GetByIdAsync(uint idItem)
        {
            await using var db = new ServerDbContext();
            return await db.Items.FirstOrDefaultAsync(x => x.Id == idItem);
        }

        public static async Task<List<DbItem>> GetBySyndicateAsync(uint idSyndicate)
        {
            await using var db = new ServerDbContext();
            return db.Items.Where(x => x.Syndicate == idSyndicate).ToList();
        }
    }
}