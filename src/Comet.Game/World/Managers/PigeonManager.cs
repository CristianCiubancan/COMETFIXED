using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.World.Managers
{
    public static class PigeonManager
    {
        private const int PIGEON_PRICE = 5;
        private const int PIGEON_ADDITION = 5;
        private const int PIGEON_TOP_ADDITION = 15;
        private const int PIGEON_MAX_MSG_LENGTH = 80;
        private const int PIGEON_STAND_SECS = 60;

        private static readonly List<DbPigeon> m_past = new();
        private static List<DbPigeonQueue> m_queue = new();
        private static DbPigeon m_current;

        private static readonly TimeOut m_next = new(PIGEON_STAND_SECS);

        public static async Task<bool> InitializeAsync()
        {
            m_queue = new List<DbPigeonQueue>((await PigeonQueueRepository.GetAsync()).OrderBy(x => x.NextIdentity));
            await SyncAsync();
            return true;
        }

        public static async Task<bool> PushAsync(Character sender, string message, bool showError = true,
                                                 bool forceShow = false)
        {
            if (message.Length > PIGEON_MAX_MSG_LENGTH)
            {
                if (showError)
                    await sender.SendAsync(Language.StrPigeonSendErrStringTooLong);
                return false;
            }

            if (string.IsNullOrEmpty(message))
            {
                if (showError)
                    await sender.SendAsync(Language.StrPigeonSendErrEmptyString);
                return false;
            }

            if (OnQueueByUser(sender.Identity) >= 5 && !sender.IsGm())
            {
                if (showError)
                    await sender.SendAsync(Language.StrPigeonSendOver5Pieces);
                return false;
            }

            if (!await sender.SpendConquerPointsAsync(PIGEON_PRICE, true))
            {
                if (showError)
                    await sender.SendAsync(Language.StrPigeonUrgentErrNoEmoney);
                return false;
            }

            var pigeon = new DbPigeonQueue
            {
                UserIdentity = sender.Identity,
                UserName = sender.Name,
                Message = message.Substring(0, Math.Min(message.Length, PIGEON_MAX_MSG_LENGTH)),
                Addition = 0,
                NextIdentity = 0
            };
            await ServerDbContext.SaveAsync(pigeon);

            m_queue.Add(pigeon);

            if (forceShow || m_next.IsTimeOut(PIGEON_STAND_SECS))
                await SyncAsync();

            await RebuildQueueAsync();
            await sender.SendAsync(Language.StrPigeonSendProducePrompt);
            return true;
        }

        public static async Task AdditionAsync(Character sender, MsgPigeon request)
        {
            DbPigeonQueue pigeon = null;
            var position = 0;

            for (var i = 0; i < m_queue.Count; i++)
                if (m_queue[i].Identity == request.Param &&
                    m_queue[i].UserIdentity == sender.Identity)
                {
                    position = i;
                    pigeon = m_queue[i];
                    break;
                }

            if (pigeon == null)
                return;

            var newPos = 0;
            switch (request.Mode)
            {
                case MsgPigeon<Client>.PigeonMode.Urgent:
                    if (!await sender.SpendConquerPointsAsync(PIGEON_ADDITION))
                    {
                        await sender.SendAsync(Language.StrPigeonUrgentErrNoEmoney);
                        return;
                    }

                    pigeon.Addition += PIGEON_ADDITION;
                    newPos = Math.Max(0, position - 5);
                    break;
                case MsgPigeon<Client>.PigeonMode.SuperUrgent:
                    if (!await sender.SpendConquerPointsAsync(PIGEON_TOP_ADDITION))
                    {
                        await sender.SendAsync(Language.StrPigeonUrgentErrNoEmoney);
                        return;
                    }

                    pigeon.Addition += PIGEON_TOP_ADDITION;
                    newPos = 0;
                    break;
            }

            m_queue.RemoveAt(position);
            m_queue.Insert(newPos, pigeon);

            await RebuildQueueAsync();
            await sender.SendAsync(Language.StrPigeonSendProducePrompt);
        }

        public static Task RebuildQueueAsync()
        {
            uint idx = 0;
            foreach (DbPigeonQueue queued in m_queue) queued.NextIdentity = idx++;
            return ServerDbContext.SaveAsync(m_queue);
        }

        public static async Task SyncAsync()
        {
            if (m_queue.Count == 0)
                return;

            m_next.Startup(PIGEON_STAND_SECS);

            await ServerDbContext.SaveAsync(m_current = new DbPigeon
            {
                UserIdentity = m_queue[0].UserIdentity,
                UserName = m_queue[0].UserName,
                Addition = m_queue[0].Addition,
                Message = m_queue[0].Message,
                Time = DateTime.Now
            });
            await ServerDbContext.DeleteAsync(m_queue[0]);

            m_queue.RemoveAt(0);
            m_past.Add(m_current);

            await RebuildQueueAsync();
            await RoleManager.BroadcastMsgAsync(new MsgTalk(m_current.UserIdentity, TalkChannel.Broadcast, Color.White,
                                                            MsgTalk.ALLUSERS, m_current.UserName, m_current.Message));
        }

        public static async Task OnTimerAsync()
        {
            if (m_queue.Count == 0)
                return;

            if (!m_next.ToNextTime(PIGEON_STAND_SECS))
                return;

            await SyncAsync();
        }

        public static async Task SendListAsync(Character user, MsgPigeon<Client>.PigeonMode request, int page)
        {
            const int ipp = 10;
            List<DbPigeonQueue> temp;
            if (request == MsgPigeon<Client>.PigeonMode.Query)
                temp = new List<DbPigeonQueue>(m_queue);
            else
                temp = new List<DbPigeonQueue>(m_queue.FindAll(x => x.UserIdentity == user.Identity));

            var pos = (uint) (page * ipp);
            var msg = new MsgPigeonQuery
            {
                Mode = (uint) page
            };

            DbPigeonQueue[] queryList = temp.Skip((int) pos).Take(ipp).ToArray();
            msg.Total = 0;
            foreach (DbPigeonQueue pigeon in queryList)
            {
                if (msg.Messages.Count >= 5)
                {
                    await user.SendAsync(msg);
                    msg.Messages.Clear();
                }

                msg.Messages.Add(new MsgPigeonQuery<Client>.PigeonMessage
                {
                    Identity = pigeon.Identity,
                    UserIdentity = pigeon.UserIdentity,
                    UserName = pigeon.UserName,
                    Addition = pigeon.Addition,
                    Message = pigeon.Message,
                    Position = pos++
                });
            }

            msg.Total = 5;
            if (msg.Messages.Count > 0)
                await user.SendAsync(msg);
        }

        public static async Task SendToUserAsync(Character user)
        {
            if (m_current != null)
                await user.SendAsync(new MsgTalk(m_current.UserIdentity, TalkChannel.Broadcast, Color.White,
                                                 MsgTalk.ALLUSERS, m_current.UserName, m_current.Message));
        }

        public static int OnQueueByUser(uint idUser)
        {
            return m_queue.Count(x => x.UserIdentity == idUser);
        }
    }
}