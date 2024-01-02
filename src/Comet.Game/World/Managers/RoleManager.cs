using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Internal.Auth;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Game.States.Items;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Network.Packets.Internal;
using Comet.Shared;

namespace Comet.Game.World.Managers
{
    public static class RoleManager
    {
        private static readonly TimeOutMS mFloorItemCheckMs = new();

        private static readonly ConcurrentDictionary<uint, Character> m_userSet = new();
        private static readonly ConcurrentDictionary<uint, Role> m_roleSet = new();
        private static readonly ConcurrentDictionary<uint, MapItem> m_mapItemSet = new();
        private static readonly List<DbDisdain> mDisdains = new();
        private static readonly Dictionary<uint, DbMonstertype> mDicMonsterTypes = new();
        private static readonly Dictionary<uint, DbMonsterTypeMagic> mMonsterMagics = new();

        private static bool m_isShutdown;

        public static int OnlineUniquePlayers => m_userSet.Values.Select(x => x.Client.IpAddress).Distinct().Count();
        public static int OnlinePlayers => m_userSet.Count;
        public static int RolesCount => m_roleSet.Count;

        public static int MaxOnlinePlayers { get; private set; }

        public static async Task<bool> InitializeAsync()
        {
            foreach (DbMonstertype mob in await MonsterypeRepository.GetAsync()) mDicMonsterTypes.TryAdd(mob.Id, mob);

            mDisdains.AddRange(await DisdainRepository.GetAsync());

            foreach (DbMonsterTypeMagic magic in await MonsterTypeMagicRepository.GetAsync())
                mMonsterMagics.TryAdd(magic.Id, magic);

            mFloorItemCheckMs.Startup(1000);

            await OfflinePlayersResetAsync();
            return true;
        }

        public static async Task<bool> LoginUserAsync(Client user)
        {
            if (m_isShutdown)
            {
                await user.SendAsync(new MsgConnectEx(MsgConnectEx<Client>.RejectionCode.ServerDown));
                user.Disconnect();
                return false;
            }

            if (m_userSet.TryGetValue(user.Character.Identity, out Character concurrent))
            {
                await Log.WriteLogAsync(LogLevel.Info,
                                        $"User {user.Character.Identity} {user.Character.Name} tried to login an already connected client.");

                if (user.IpAddress != concurrent.Client.IpAddress)
                    await concurrent.SendAsync(Language.StrAnotherLoginSameIp, TalkChannel.Talk);
                else
                    await concurrent.SendAsync(Language.StrAnotherLoginOtherIp, TalkChannel.Talk);

                concurrent.Client.Disconnect();
                user.Disconnect();
                //await KickOutAsync(user.Character.Identity, "logged twice");
                return false;
            }

            if (m_userSet.Count > Kernel.GameConfiguration.MaxConn && user.AccountIdentity >= 10000 &&
                !user.Character.IsGm())
            {
                await user.SendAsync(new MsgConnectEx(MsgConnectEx<Client>.RejectionCode.ServerFull));
                await Log.WriteLogAsync(LogLevel.Warning, $"{user.Character.Name} tried to login and server is full.");
                user.Disconnect();
                return false;
            }

            m_userSet.TryAdd(user.Character.Identity, user.Character);
            m_roleSet.TryAdd(user.Character.Identity, user.Character);

            await user.Character.SetLoginAsync();

            await Log.WriteLogAsync(LogLevel.Info, $"{user.Character.Name} has logged in.");

            await Kernel.AccountServer.SendAsync(new MsgAccServerPlayerStatus
            {
                ServerName = Kernel.GameConfiguration.ServerName,
                Status = new List<MsgAccServerPlayerStatus<AccountServer>.PlayerStatus>
                {
                    new() {Identity = user.AccountIdentity, Online = true}
                }
            });

            {
                // scope to don't create variable externally
                var msg = new MsgAccServerPlayerExchange
                {
                    ServerName = Kernel.GameConfiguration.ServerName
                };
                msg.Data.Add(MsgAccServerPlayerExchange.CreatePlayerData(user.Character));
                await Kernel.AccountServer.SendAsync(msg);
            }

            if (OnlinePlayers > MaxOnlinePlayers)
                MaxOnlinePlayers = OnlinePlayers;
            return true;
        }

        public static void ForceLogoutUser(uint idUser)
        {
            m_userSet.TryRemove(idUser, out _);
            m_roleSet.TryRemove(idUser, out _);
        }

        public static async Task KickOutAsync(uint idUser, string reason = "")
        {
            if (m_userSet.TryGetValue(idUser, out Character user))
            {
                await user.SendAsync(string.Format(Language.StrKickout, reason), TalkChannel.Talk, Color.White);
                user.Client.Disconnect();
                await Log.WriteLogAsync(LogLevel.Info, $"User {user.Name} has been kicked: {reason}");
            }
        }

        public static async Task KickOutAllAsync(string reason = "", bool isShutdown = false)
        {
            if (isShutdown)
                m_isShutdown = true;

            foreach (Character user in m_userSet.Values)
            {
                await user.SendAsync(string.Format(Language.StrKickout, reason), TalkChannel.Talk, Color.White);
                user.Client.Disconnect();

                await Log.WriteLogAsync(LogLevel.Info, $"User {user.Name} has been kicked (kickoutall): {reason}");
            }
        }

        public static Character GetUserByAccount(uint idAccount)
        {
            return m_userSet.Values.FirstOrDefault(x => x.Client.AccountIdentity == idAccount);
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
            if (role is MapItem item)
                m_mapItemSet.TryAdd(role.Identity, item);
            return m_roleSet.TryAdd(role.Identity, role);
        }

        public static Role GetRole(uint idRole)
        {
            return m_roleSet.TryGetValue(idRole, out Role role) ? role : null;
        }

        public static List<Role> QueryRoles(Func<Role, bool> predicate)
        {
            return m_roleSet.Values.Where(predicate).ToList();
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
            m_mapItemSet.TryRemove(idRole, out _);
            return m_roleSet.TryRemove(idRole, out _);
        }

        public static Task BroadcastMsgAsync(string message, TalkChannel channel = TalkChannel.System,
                                             Color? color = null)
        {
            foreach (Character user in m_userSet.Values)
                _ = user.SendAsync(message, channel, color).ConfigureAwait(false);
            return Task.CompletedTask;
        }

        public static Task BroadcastMsgAsync(IPacket msg, uint ignore = 0)
        {
            foreach (Character user in m_userSet.Values)
            {
                if (user.Identity == ignore) continue;
                _ = user.SendAsync(msg).ConfigureAwait(false);
            }

            return Task.CompletedTask;
        }

        public static DbMonstertype GetMonstertype(uint type)
        {
            return mDicMonsterTypes.TryGetValue(type, out DbMonstertype mob) ? mob : null;
        }

        public static List<DbMonsterTypeMagic> GetMonsterMagics(uint type)
        {
            return mMonsterMagics.Values.Where(x => x.MonsterType == type).ToList();
        }

        public static DbDisdain GetDisdain(int delta)
        {
            return mDisdains.Aggregate((x, y) => Math.Abs(x.DeltaLev - delta) < Math.Abs(y.DeltaLev - delta) ? x : y);
        }

        #region OnTimer

        public static async Task OnTimerAsync()
        {
            foreach (Character user in m_userSet.Values)
            {
                if (!user.IsConnected)
                    continue;

                await user.OnTimerAsync();
            }
        }

        public static async Task OnRoleTimerAsync()
        {
            foreach (Role role in m_roleSet.Values.Where(x => !x.IsPlayer())) await role.OnTimerAsync();

            //if (mFloorItemCheckMs.ToNextTime())
            //{
            //    foreach (var item in m_mapItemSet.Values)
            //    {
            //        await item.OnTimerAsync();
            //    }
            //}
        }

        #endregion

        #region Daily Reset

        public static async Task OnDailyTriggerAsync()
        {
            var sw = Stopwatch.StartNew();
            uint today = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));

            foreach (Character user in m_userSet.Values)
                user.QueueAction(async () =>
                {
                    await DoResetAsync(user, true);
                    await user.SaveAsync();
                });

            await OfflinePlayersResetAsync();

            await EventManager.DailyAsync();
            await FlowerManager.DailyResetAsync();
            sw.Stop();
            await Log.WriteLogAsync($"[{sw.ElapsedMilliseconds / 1000d:0.000} secs] Daily Reset job executed!");
        }

        private static async Task OfflinePlayersResetAsync()
        {
            try
            {
                uint today = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
                List<DbCharacter> dbUsers = await CharactersRepository.GetResetAsync(today);
                foreach (Character user in dbUsers.Select(x => new Character(x, null)))
                    try
                    {
                        await DoResetAsync(user, false);
                    }
                    catch (Exception ex)
                    {
                        await Log.WriteLogAsync(ex);
                    }

                await ServerDbContext.SaveAsync(dbUsers);
            }
            catch (Exception ex)
            {
                await Log.WriteLogAsync(ex);
            }
        }

        private static async Task DoResetAsync(Character user, bool isOnline)
        {
            // Enlight points
            await user.ResetEnlightenmentAsync();

            // Arena Qualifier
            user.QualifierDayWins = 0;
            user.QualifierDayLoses = 0;
            user.QualifierPoints = ArenaQualifier.GetInitialPoints(user.Level);

            user.LastDailyUpdate = uint.Parse(DateTime.Now.ToString("yyyyMMdd"));
        }

        #endregion
    }
}