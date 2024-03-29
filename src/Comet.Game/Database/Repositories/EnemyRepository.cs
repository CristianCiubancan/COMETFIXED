﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;

namespace Comet.Game.Database.Repositories
{
    public static class EnemyRepository
    {
        public static async Task<List<DbEnemy>> GetAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.Enemies.Where(x => x.UserIdentity == idUser).ToList();
        }

        public static async Task<List<DbEnemy>> GetOwnEnemyAsync(uint idUser)
        {
            await using var db = new ServerDbContext();
            return db.Enemies.Where(x => x.TargetIdentity == idUser).ToList();
        }
    }
}