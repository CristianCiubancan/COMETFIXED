using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.AI.Database.Repositories;
using Comet.AI.States;
using Comet.AI.World.Maps;
using Comet.Database.Entities;

namespace Comet.AI.World.Managers
{
    public static class RoleManager
    {
        public static int RolesCount => m_roleSet.Count;

        public static async Task<bool> InitializeAsync()
        {
            foreach (DbMonstertype mob in await MonsterypeRepository.GetAsync()) mDicMonsterTypes.TryAdd(mob.Id, mob);
            foreach (DbMonsterTypeMagic magic in await MonsterTypeMagicRepository.GetAsync())
                mMonsterMagics.TryAdd(magic.Id, magic);
            return true;
        }

        public static bool LoginUser(Character user)
        {
            m_userSet.TryAdd(user.Identity, user);
            return true;
        }

        public static bool LogoutUser(uint idUser, out Character user)
        {
            if (!m_userSet.TryRemove(idUser, out user))
                return false;
            return true;
        }

        public static Character GetUser(uint idUser)
        {
            return m_userSet.TryGetValue(idUser, out Character client) ? client : null;
        }

        public static Character GetUser(string name)
        {
            return m_userSet.Values.FirstOrDefault(x => x.Name == name);
        }

        public static List<T> QueryRoleByMap<T>(uint idMap) where T : Role
        {
            return m_roleSet.Values.Where(x => x.MapIdentity == idMap && x is T).Cast<T>().ToList();
        }

        public static List<T> QueryRoleByType<T>() where T : Role
        {
            return m_roleSet.Values.Where(x => x is T).Cast<T>().ToList();
        }

        public static List<Character> QueryUserSetByMap(uint idMap)
        {
            return m_userSet.Values.Where(x => x.MapIdentity == idMap).ToList();
        }

        public static List<Character> QueryUserSet()
        {
            return m_userSet.Values.ToList();
        }

        /// <summary>
        ///     Attention, DO NOT USE to add <see cref="Character" />.
        /// </summary>
        public static bool AddRole(Role role)
        {
            return m_roleSet.TryAdd(role.Identity, role);
        }

        public static Role GetRole(uint idRole)
        {
            return m_roleSet.TryGetValue(idRole, out Role role) ? role : null;
        }

        public static T GetRole<T>(uint idRole) where T : Role
        {
            return m_roleSet.TryGetValue(idRole, out Role role) ? role as T : null;
        }

        public static T GetRole<T>(Func<T, bool> predicate) where T : Role
        {
            return m_roleSet.Values
                            .Where(x => x is T)
                            .Cast<T>()
                            .FirstOrDefault(x => predicate != null && predicate(x));
        }

        public static T FindRole<T>(uint idRole) where T : Role
        {
            foreach (GameMap map in MapManager.GameMaps.Values)
            {
                var result = map.QueryRole<T>(idRole);
                if (result != null)
                    return result;
            }

            return null;
        }

        public static T FindRole<T>(Func<T, bool> predicate) where T : Role
        {
            foreach (GameMap map in MapManager.GameMaps.Values)
            {
                T result = map.QueryRole(predicate);
                if (result != null)
                    return result;
            }

            return null;
        }

        /// <summary>
        ///     Attention, DO NOT USE to remove <see cref="Character" />.
        /// </summary>
        public static bool RemoveRole(uint idRole)
        {
            return m_roleSet.TryRemove(idRole, out _);
        }

        public static DbMonstertype GetMonstertype(uint type)
        {
            return mDicMonsterTypes.TryGetValue(type, out DbMonstertype mob) ? mob : null;
        }

        public static List<DbMonsterTypeMagic> GetMonsterMagics(uint type)
        {
            return mMonsterMagics.Values.Where(x => x.MonsterType == type).ToList();
        }

        #region OnTimer

        public static async Task OnTimerAsync()
        {
            foreach (Role role in m_roleSet.Values.Where(x => !x.IsPlayer()
                                                              && x.IsAlive
                                                              && x.Map?.PlayerCount > 0))
                await role.OnTimerAsync();
        }

        #endregion

        private static readonly ConcurrentDictionary<uint, Character> m_userSet = new();
        private static readonly ConcurrentDictionary<uint, Role> m_roleSet = new();
        private static readonly Dictionary<uint, DbMonstertype> mDicMonsterTypes = new();
        private static readonly Dictionary<uint, DbMonsterTypeMagic> mMonsterMagics = new();
    }
}