using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class LevelExperienceRepository
    {
        public static async Task<List<DbLevelExperience>> GetAsync()
        {
            await using var db = new ServerDbContext();
            return db.LevelExperience.ToList();
        }
    }
}