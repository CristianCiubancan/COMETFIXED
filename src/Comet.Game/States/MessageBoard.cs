using System;
using System.Collections.Generic;
using System.Linq;
using Comet.Network.Packets.Game;

namespace Comet.Game.States
{
    public static class MessageBoard
    {
        private static readonly Dictionary<uint, MessageInfo> m_dicTrade = new();
        private static readonly Dictionary<uint, MessageInfo> m_dicTTeam = new();
        private static readonly Dictionary<uint, MessageInfo> m_dicFriend = new();
        private static readonly Dictionary<uint, MessageInfo> m_dicSyndicate = new();
        private static readonly Dictionary<uint, MessageInfo> m_dicOther = new();
        private static readonly Dictionary<uint, MessageInfo> m_dicSystem = new();

        public static bool AddMessage(Character user, string message, TalkChannel channel)
        {
            Dictionary<uint, MessageInfo> board;
            switch (channel)
            {
                case TalkChannel.TradeBoard:
                    board = m_dicTrade;
                    break;
                case TalkChannel.TeamBoard:
                    board = m_dicTTeam;
                    break;
                case TalkChannel.FriendBoard:
                    board = m_dicFriend;
                    break;
                case TalkChannel.GuildBoard:
                    board = m_dicSyndicate;
                    break;
                case TalkChannel.OthersBoard:
                    board = m_dicOther;
                    break;
                case TalkChannel.Bbs:
                    board = m_dicSystem;
                    break;
                default:
                    return false;
            }

            if (board.ContainsKey(user.Identity))
                board.Remove(user.Identity);

            // todo verify silence
            // todo filter words

            board.Add(user.Identity, new MessageInfo
            {
                SenderIdentity = user.Identity,
                Sender = user.Name,
                Message = message.Substring(0, Math.Min(message.Length, 255)),
                Time = DateTime.Now
            });

            return true;
        }

        public static List<MessageInfo> GetMessages(TalkChannel channel, int page)
        {
            List<MessageInfo> msgs;
            switch (channel)
            {
                case TalkChannel.TradeBoard:
                    msgs = m_dicTrade.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.TeamBoard:
                    msgs = m_dicTTeam.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.FriendBoard:
                    msgs = m_dicFriend.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.GuildBoard:
                    msgs = m_dicSyndicate.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.OthersBoard:
                    msgs = m_dicOther.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.Bbs:
                    msgs = m_dicSystem.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                default:
                    return new List<MessageInfo>();
            }

            if (page * 8 > msgs.Count)
                return new List<MessageInfo>();

            return msgs.Skip(page * 8).Take(8).ToList();
        }

        public static string GetMessage(string name, TalkChannel channel)
        {
            List<MessageInfo> msgs;
            switch (channel)
            {
                case TalkChannel.TradeBoard:
                    msgs = m_dicTrade.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.TeamBoard:
                    msgs = m_dicTTeam.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.FriendBoard:
                    msgs = m_dicFriend.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.GuildBoard:
                    msgs = m_dicSyndicate.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.OthersBoard:
                    msgs = m_dicOther.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                case TalkChannel.Bbs:
                    msgs = m_dicSystem.Values.OrderByDescending(x => x.Time).ToList();
                    break;
                default:
                    return string.Empty;
            }

            return msgs.FirstOrDefault(x => x.Sender.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                       .Message ?? string.Empty;
        }
    }

    public struct MessageInfo
    {
        public uint SenderIdentity;
        public string Sender;
        public string Message;
        public DateTime Time;
    }
}