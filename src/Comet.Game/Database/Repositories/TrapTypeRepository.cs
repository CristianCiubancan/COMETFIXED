using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class TrapTypeRepository
    {
        public static async Task<DbTrapType> GetAsync(uint id)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.TrapTypes.FindAsync(id);
        }
    }
}