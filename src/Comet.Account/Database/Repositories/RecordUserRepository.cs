using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Account.Database.Repositories
{
    public static class RecordUserRepository
    {
        public static async Task<DbRecordUser> GetAsync(uint idSyn, uint idServer)
        {
            await using ServerDbContext ctx = new();
            return await ctx.RecordUsers.FirstOrDefaultAsync(x =>
                                                                 x.UserIdentity == idSyn &&
                                                                 x.ServerIdentity == idServer);
        }

        public static async Task<List<DbRecordUser>> GetAsync(uint idServer, int limit, int from = 0)
        {
            await using ServerDbContext ctx = new();
            if (idServer == 0)
                return await ctx.RecordUsers.Where(x => x.DeletedAt == null).Skip(from).Take(limit).ToListAsync();
            return await ctx.RecordUsers.Where(x => x.ServerIdentity == idServer && x.DeletedAt == null).Skip(from)
                            .Take(limit).ToListAsync();
        }

        public static async Task<DbRecordUser> GetByIdAsync(uint idUser, uint idServer)
        {
            await using ServerDbContext ctx = new();
            return await ctx.RecordUsers
                            .FirstOrDefaultAsync(x => x.UserIdentity == idUser && x.ServerIdentity == idServer);
        }
    }
}