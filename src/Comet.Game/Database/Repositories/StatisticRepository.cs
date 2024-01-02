using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class StatisticRepository
    {
        public static async Task<List<DbStatistic>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.Statistic.Where(x => x.PlayerIdentity == idUser).ToList();
        }
    }
}