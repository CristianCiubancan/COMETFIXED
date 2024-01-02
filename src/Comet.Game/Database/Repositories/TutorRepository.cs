using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class TutorRepository
    {
        public static async Task<DbTutor> GetAsync(uint idStudent)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Tutor
                            .Include(x => x.Guide)
                            .Include(x => x.Student)
                            .FirstOrDefaultAsync(x => x.StudentId == idStudent);
        }

        public static async Task<List<DbTutor>> GetStudentsAsync(uint idTutor)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Tutor
                            .Include(x => x.Guide)
                            .Include(x => x.Student)
                            .Where(x => x.GuideId == idTutor)
                            .ToListAsync();
        }
    }
}