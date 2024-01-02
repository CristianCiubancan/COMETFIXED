using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class GeneratorRepository
    {
        public static async Task<List<DbGenerator>> GetAsync()
        {
            await using var context = new ServerDbContext();
            return context.Generator.ToList();
        }
    }
}