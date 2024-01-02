using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.States.Relationship
{
    public sealed class Enemy
    {
        private DbEnemy m_DbEnemy;
        private readonly Character m_owner;

        public Enemy(Character owner)
        {
            m_owner = owner;
        }

        public uint Identity => m_DbEnemy.TargetIdentity;
        public string Name { get; private set; }
        public bool Online => User != null;
        public Character User => RoleManager.GetUser(Identity);

        public async Task<bool> CreateAsync(Character user)
        {
            m_DbEnemy = new DbEnemy
            {
                UserIdentity = m_owner.Identity,
                TargetIdentity = user.Identity,
                Time = DateTime.Now
            };
            Name = user.Name;
            await SendAsync();
            return await SaveAsync();
        }

        public async Task CreateAsync(DbEnemy enemy)
        {
            m_DbEnemy = enemy;
            Name = (await CharactersRepository.FindByIdentityAsync(enemy.TargetIdentity))?.Name ?? Language.StrNone;
        }

        public async Task SendAsync()
        {
            await m_owner.SendAsync(new MsgFriend
            {
                Identity = Identity,
                Name = Name,
                Action = MsgFriend<Client>.MsgFriendAction.AddEnemy,
                Online = Online
            });
        }

        public async Task SendInfoAsync()
        {
            Character user = User;
            await m_owner.SendAsync(new MsgFriendInfo
            {
                Identity = Identity,
                PkPoints = user?.PkPoints ?? 0,
                Level = user?.Level ?? 0,
                Mate = user?.MateName ?? Language.StrNone,
                Profession = user?.Profession ?? 0,
                Lookface = user?.Mesh ?? 0,
                IsEnemy = true
            });
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                await using var ctx = new ServerDbContext();
                if (m_DbEnemy.Identity == 0)
                    await ctx.Enemies.AddAsync(m_DbEnemy);
                else
                    ctx.Enemies.Update(m_DbEnemy);
                await ctx.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }

        public async Task<bool> DeleteAsync()
        {
            try
            {
                await using var ctx = new ServerDbContext();
                if (m_DbEnemy.Identity == 0)
                    return false;
                ctx.Enemies.Remove(m_DbEnemy);
                await ctx.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(LogLevel.Exception, ex.ToString());
                return false;
            }
        }
    }
}