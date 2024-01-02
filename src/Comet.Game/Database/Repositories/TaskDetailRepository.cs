using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class TaskDetailRepository
    {
        public static async Task<List<DbTaskDetail>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.TaskDetail.Where(x => x.UserIdentity == idUser).ToList();
        }
    }
}