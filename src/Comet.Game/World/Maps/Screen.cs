using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Network.Packets;
using Comet.Shared;

namespace Comet.Game.World.Maps
{
    public sealed class Screen
    {
        public const int VIEW_SIZE = 18;
        public const int BROADCAST_SIZE = 21;

        private readonly Role mRole;

        public ConcurrentDictionary<uint, Role> Roles = new();

        public Screen(Role role)
        {
            mRole = role;
        }

        public bool Add(Role role)
        {
            return Roles.TryAdd(role.Identity, role);
        }

        public async Task RemoveAsync(uint idRole, bool force = false)
        {
            Roles.TryRemove(idRole, out Role role);

            if (force)
            {
                var msg = new MsgAction
                {
                    Identity = idRole,
                    Action = MsgAction<Client>.ActionType.RemoveEntity
                };
                await mRole.SendAsync(msg);
            }
        }

        public async Task SynchroScreenAsync()
        {
            if (mRole is not Character player)
                return;

            foreach (Role role in Roles.Values)
            {
                await role.SendSpawnToAsync(player);

                if (role is Character user)
                    await mRole.SendSpawnToAsync(user);
            }
        }

        public async Task UpdateAsync(IPacket msg = null)
        {
            var isJmpMsg = false;
            ushort oldX = 0;
            ushort oldY = 0;
            var dda = new List<Point>();
            if (msg is MsgAction jump && jump.Action == MsgAction<Client>.ActionType.MapJump)
            {
                isJmpMsg = true;

                oldX = jump.X;
                oldY = jump.Y;

                Calculations.DDALine(oldX, oldY, mRole.MapX, mRole.MapY, VIEW_SIZE, ref dda);
            }
            else
            {
                jump = null;
            }

            List<Role> targets = mRole.Map.Query9BlocksByPos(mRole.MapX, mRole.MapY);
            targets.AddRange(Roles.Values);
            foreach (Role target in targets.Select(x => x).Distinct())
            {
                if (target.Identity == mRole.Identity) continue;

                ushort newOldX = oldX;
                ushort newOldY = oldY;
                var isExit = false;
                var targetUser = target as Character;
                if (Calculations.GetDistance(mRole.MapX, mRole.MapY, target.MapX, target.MapY) <= mRole.ViewRange)
                {
                    /*
                     * I add the target to my screen and it doesn't matter if he already sees me, I'll try to add myself into his screen.
                     * If succcess, I exchange the spawns.
                     */
                    if (Add(target))
                    {
                        target.Screen?.Add(mRole);

                        if (mRole is Character user)
                            await target.SendSpawnToAsync(user);

                        if (targetUser != null && isJmpMsg)
                        {
                            for (var i = 0; i < dda.Count; i++)
                                if (targetUser.GetDistance(dda[i].X, dda[i].Y) < VIEW_SIZE)
                                {
                                    newOldX = (ushort) dda[i].X;
                                    newOldY = (ushort) dda[i].Y;
                                    break;
                                }

                            await mRole.SendSpawnToAsync(targetUser, newOldX, newOldY);
                        }
                        else if (targetUser != null)
                        {
                            await mRole.SendSpawnToAsync(targetUser);
                        }
                    }
                }
                else
                {
                    isExit = true;
                    await RemoveAsync(target.Identity);
                    if (target.Screen != null)
                        await target.Screen.RemoveAsync(mRole.Identity);
                }

                if (msg != null && targetUser != null)
                {
                    if (isJmpMsg && !isExit)
                        await targetUser.SendAsync(new MsgAction
                        {
                            Action = jump.Action,
                            Argument = jump.Argument,
                            X = newOldX,
                            Y = newOldY,
                            Command = jump.Command,
                            Data = jump.Data,
                            Direction = jump.Direction,
                            Identity = jump.Identity,
                            Map = jump.Map,
                            MapColor = jump.MapColor,
                            Timestamp = jump.Timestamp
                        });
                    else
                        await targetUser.SendAsync(msg);
                }
            }
        }

        public async Task BroadcastRoomMsgAsync(IPacket msg, bool self = true)
        {
            if (self && mRole != null)
                await mRole.SendAsync(msg);

            foreach (Character target in Roles.Values.Where(x => x is Character).Cast<Character>())
                await target.SendAsync(msg);
        }

        /// <summary>
        ///     For roles (not users) entering the screen.
        /// </summary>
        public async Task<bool> SpawnAsync(Role role)
        {
            if (mRole is not Character user)
                return false;

            if (Roles.TryAdd(role.Identity, role))
            {
                await role.SendSpawnToAsync(user);
                return true;
            }

            return false;
        }

        public async Task ClearAsync(bool sync = false)
        {
            if (sync && mRole is Character)
                foreach (Role role in Roles.Values)
                {
                    var msg = new MsgAction
                    {
                        Identity = role.Identity,
                        Action = MsgAction<Client>.ActionType.RemoveEntity
                    };
                    await mRole.SendAsync(msg);
                }

            Roles.Clear();
        }
    }
}