using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class TaskRepository
    {
        public static async Task<List<DbTask>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.Tasks.ToList();
        }
    }
}