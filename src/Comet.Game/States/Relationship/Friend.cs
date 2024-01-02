using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.States.Relationship
{
    public sealed class Friend
    {
        private DbFriend m_dbFriend;
        private readonly Character m_owner;

        public Friend(Character owner)
        {
            m_owner = owner;
        }

        public uint Identity => m_dbFriend.TargetIdentity;
        public string Name { get; private set; }
        public bool Online => User != null;
        public Character User => RoleManager.GetUser(Identity);

        public bool Create(Character user)
        {
            m_dbFriend = new DbFriend
            {
                UserIdentity = m_owner.Identity,
                TargetIdentity = user.Identity,
                Time = DateTime.Now
            };
            Name = user.Name;
            return true;
        }

        public async Task CreateAsync(DbFriend friend)
        {
            m_dbFriend = friend;
            Name = (await CharactersRepository.FindByIdentityAsync(friend.TargetIdentity))?.Name ?? Language.StrNone;
        }

        public async Task SendAsync()
        {
            await m_owner.SendAsync(new MsgFriend
            {
                Identity = Identity,
                Name = Name,
                Action = MsgFriend<Client>.MsgFriendAction.AddFriend,
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
                SyndicateIdentity = user?.SyndicateIdentity ?? 0,
                SyndicateRank = (ushort) (user?.SyndicateRank ?? SyndicateMember.SyndicateRank.None)
            });
        }

        public async Task<bool> SaveAsync()
        {
            try
            {
                await using var ctx = new ServerDbContext();
                if (m_dbFriend.Identity == 0)
                    await ctx.Friends.AddAsync(m_dbFriend);
                else
                    ctx.Friends.Update(m_dbFriend);
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
                if (m_dbFriend.Identity == 0)
                    return false;
                ctx.Friends.Remove(m_dbFriend);
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