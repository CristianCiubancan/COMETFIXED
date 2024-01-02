using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class GoodsRepository
    {
        public static async Task<List<DbGoods>> GetAsync(uint idNpc)
        {
            await using var context = new ServerDbContext();
            return context.Goods.Where(x => x.OwnerIdentity == idNpc).ToList();
        }
    }
}