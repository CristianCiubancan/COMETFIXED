using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class PortalRepository
    {
        public static async Task<DbPortal> GetAsync(uint idMap, uint idx)
        {
            await using var context = new ServerDbContext();
            return context.Portal.FirstOrDefault(x => x.MapId == idMap && x.PortalIndex == idx);
        }
    }
}