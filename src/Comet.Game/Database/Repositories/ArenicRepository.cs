using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Microsoft.EntityFrameworkCore;

namespace Comet.Game.Database.Repositories
{
    public static class ArenicRepository
    {
        public static async Task<List<DbArenic>> GetAsync(DateTime date)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                            .Where(x => x.Date == date.Date)
                            .ToListAsync();
        }

        public static async Task<List<DbArenic>> GetRankAsync(int from, int limit = 10)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                            .Where(x => x.Date == DateTime.Now.Date)
                            .OrderByDescending(x => x.AthletePoint)
                            .ThenByDescending(x => x.DayWins)
                            .ThenBy(x => x.DayLoses)
                            .Skip(from)
                            .Take(limit)
                            .Include(x => x.User)
                            .ToListAsync();
        }

        public static async Task<int> GetRankCountAsync()
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                            .Where(x => x.Date == DateTime.Now.Date)
                            .OrderByDescending(x => x.AthletePoint)
                            .ThenByDescending(x => x.DayWins)
                            .ThenBy(x => x.DayLoses)
                            .CountAsync();
        }

        public static async Task<List<DbArenic>> GetSeasonRankAsync(DateTime date)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                            .Where(x => x.Date == date.Date)
                            .OrderByDescending(x => x.AthletePoint)
                            .ThenByDescending(x => x.DayWins)
                            .ThenBy(x => x.DayLoses)
                            .Take(10)
                            .Include(x => x.User)
                            .ToListAsync();
        }

        public static async Task<int> GetSeasonRankCountAsync(DateTime date)
        {
            await using var ctx = new ServerDbContext();
            return await ctx.Arenics
                            .Where(x => x.Date == date.Date)
                            .OrderByDescending(x => x.AthletePoint)
                            .ThenByDescending(x => x.DayWins)
                            .ThenBy(x => x.DayLoses)
                            .CountAsync();
        }
    }
}