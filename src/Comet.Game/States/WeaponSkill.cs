using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;

namespace Comet.Game.States
{
    public sealed class WeaponSkill
    {
        private readonly Character m_user;
        private readonly ConcurrentDictionary<ushort, DbWeaponSkill> m_skills;

        public WeaponSkill(Character user)
        {
            m_user = user;
            m_skills = new ConcurrentDictionary<ushort, DbWeaponSkill>();
        }

        public async Task InitializeAsync()
        {
            foreach (DbWeaponSkill skill in await WeaponSkillRepository.GetAsync(m_user.Identity))
                m_skills.TryAdd((ushort) skill.Type, skill);
        }

        public DbWeaponSkill this[ushort type] => m_skills.TryGetValue(type, out DbWeaponSkill item) ? item : null;

        public async Task<bool> CreateAsync(ushort type, byte level = 1)
        {
            if (m_skills.ContainsKey(type))
                return false;

            var skill = new DbWeaponSkill
            {
                Type = type,
                Experience = 0,
                Level = level,
                OwnerIdentity = m_user.Identity,
                OldLevel = 0,
                Unlearn = 0
            };

            if (await SaveAsync(skill))
            {
                await m_user.SendAsync(new MsgWeaponSkill
                {
                    Experience = skill.Experience,
                    Level = skill.Level,
                    Identity = skill.Type
                });
                return m_skills.TryAdd(type, skill);
            }

            return false;
        }

        public Task<bool> SaveAsync(DbWeaponSkill skill)
        {
            return ServerDbContext.SaveAsync(skill);
        }

        public Task<bool> SaveAllAsync()
        {
            return ServerDbContext.SaveAsync(m_skills.Values.ToList());
        }

        public async Task<bool> UnearnAllAsync()
        {
            foreach (DbWeaponSkill skill in m_skills.Values)
            {
                skill.Unlearn = 1;
                skill.OldLevel = skill.Level;
                skill.Level = 0;
                skill.Experience = 0;

                await m_user.SendAsync(new MsgAction
                {
                    Action = MsgAction<Client>.ActionType.ProficiencyRemove,
                    Identity = m_user.Identity,
                    Command = skill.Type,
                    Argument = skill.Type
                });
            }

            return true;
        }

        public async Task SendAsync(DbWeaponSkill skill)
        {
            await m_user.SendAsync(new MsgWeaponSkill
            {
                Experience = skill.Experience,
                Level = skill.Level,
                Identity = skill.Type
            });
        }

        public async Task SendAsync()
        {
            foreach (DbWeaponSkill skill in m_skills.Values.Where(x => x.Unlearn == 0))
                await m_user.SendAsync(new MsgWeaponSkill
                {
                    Experience = skill.Experience,
                    Level = skill.Level,
                    Identity = skill.Type
                });
        }
    }
}