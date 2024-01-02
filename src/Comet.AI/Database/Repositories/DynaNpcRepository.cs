using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.AI.Database.Repositories
{
    public static class DynaNpcRespository
    {
        public static async Task<List<DbDynanpc>> GetAsync()
        {
            await using var context = new ServerDbContext();
            return context.DynaNpcs.ToList();
        }
    }
}