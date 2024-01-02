using System;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.World.Managers;
using Comet.Shared;

namespace Comet.Game.States.Families
{
    public sealed class FamilyMember
    {
        private DbFamilyAttr m_attr;

        private FamilyMember()
        {
        }

        #region Static Creation

        public static async Task<FamilyMember> CreateAsync(Character player, Family family,
                                                           Family.FamilyRank rank = Family.FamilyRank.Member,
                                                           uint proffer = 0)
        {
            if (player == null || family == null || rank == Family.FamilyRank.None)
                return null;

            var attr = new DbFamilyAttr
            {
                FamilyIdentity = family.Identity,
                UserIdentity = player.Identity,
                Proffer = proffer,
                AutoExercise = 0,
                ExpDate = 0,
                JoinDate = DateTime.Now,
                Rank = (byte) rank
            };
            if (!await ServerDbContext.SaveAsync(attr))
                return null;

            var member = new FamilyMember
            {
                m_attr = attr,
                Name = player.Name,
                MateIdentity = player.MateIdentity,
                Level = player.Level,
                LookFace = player.Mesh,
                Profession = player.Profession,

                FamilyIdentity = family.Identity,
                FamilyName = family.Name
            };
            if (!await member.SaveAsync())
                return null;

            await Log.GmLogAsync(
                "family", $"[{player.Identity}],[{player.Name}],[{family.Identity}],[{family.Name}],[Join]");
            return member;
        }

        public static async Task<FamilyMember> CreateAsync(DbFamilyAttr player, Family family)
        {
            DbCharacter dbUser = await CharactersRepository.FindByIdentityAsync(player.UserIdentity);
            if (dbUser == null)
                return null;

            var member = new FamilyMember
            {
                m_attr = player,
                Name = dbUser.Name,
                MateIdentity = dbUser.Mate,
                Level = dbUser.Level,
                LookFace = dbUser.Mesh,
                Profession = dbUser.Profession,

                FamilyIdentity = family.Identity,
                FamilyName = family.Name
            };

            return member;
        }

        #endregion

        #region Properties

        public uint Identity => m_attr.UserIdentity;
        public string Name { get; private init; }
        public byte Level { get; set; }
        public uint MateIdentity { get; private init; }
        public uint LookFace { get; private init; }
        public ushort Profession { get; private init; }

        public Family.FamilyRank Rank
        {
            get => (Family.FamilyRank) m_attr.Rank;
            set => m_attr.Rank = (byte) value;
        }

        public DateTime JoinDate => m_attr.JoinDate;

        public uint Proffer
        {
            get => m_attr.Proffer;
            set => m_attr.Proffer = value;
        }

        public Character User => RoleManager.GetUser(Identity);

        public bool IsOnline => User != null;

        #endregion

        #region Family Properties

        public uint FamilyIdentity { get; private set; }
        public string FamilyName { get; private set; }

        #endregion

        #region Database

        public Task<bool> SaveAsync()
        {
            return ServerDbContext.SaveAsync(m_attr);
        }


        public Task<bool> DeleteAsync()
        {
            return ServerDbContext.DeleteAsync(m_attr);
        }

        #endregion
    }
}