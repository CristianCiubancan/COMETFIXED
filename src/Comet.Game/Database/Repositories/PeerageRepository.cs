using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class PeerageRepository
    {
        public static async Task<List<DbPeerage>> GetAsync()
        {
            await using var context = new ServerDbContext();
            return context.Peerage.ToList();
        }
    }
}