using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class ActionRepository
    {
        public static async Task<List<DbAction>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.Actions.ToList();
        }
    }
}