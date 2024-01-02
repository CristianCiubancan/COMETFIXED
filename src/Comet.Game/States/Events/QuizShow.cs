using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.States.Events
{
    public sealed class QuizShow : GameEvent
    {
        private const uint NPC_ID_U = 100012;
        private const int MAX_QUESTION = 20;
        private const int TIME_PER_QUESTION = 30;

        private const int TOTAL_EXP_REWARD = 600;

        private readonly ushort[] m_expReward =
        {
            3000,
            1800,
            1200,
            600
        };

        public QuizShow()
            : base("QuizShow", 500)
        {
        }

        private readonly List<DbQuiz> m_questions = new();
        private readonly List<DbQuiz> m_quizQuestions = new();
        private readonly ConcurrentDictionary<uint, QuizUser> m_users = new();

        private DynamicNpc m_owner;

        private int m_questionIdx;
        private int m_lastCorrectReply = -1;
        private readonly TimeOut m_questionTimer = new(30);

        public override EventType Identity => EventType.QuizShow;

        public QuizStatus Status { get; private set; } = QuizStatus.Idle;

        public override async Task<bool> CreateAsync()
        {
            m_owner = RoleManager.GetRole<DynamicNpc>(NPC_ID_U);
            if (m_owner == null)
            {
                await Log.WriteLogAsync(LogLevel.Error, $"Could not load NPC {NPC_ID_U} for {Name}");
                return false;
            }

            m_owner.Data0 = 0;

            m_questions.AddRange(await QuizRepository.GetAsync());
            return true;
        }

        public override async Task OnTimerAsync()
        {
            if (Status == QuizStatus.Idle)
            {
                if (m_owner.Data0 == 3) // load
                {
                    m_users.Clear();
                    m_quizQuestions.Clear();
                    var temp = new List<DbQuiz>(m_questions);
                    for (var i = 0; i < Math.Min(temp.Count, MAX_QUESTION); i++)
                    {
                        int idx = await Kernel.NextAsync(temp.Count) % Math.Max(1, temp.Count);
                        m_quizQuestions.Add(temp[idx]);
                        temp.RemoveAt(idx);
                    }

                    foreach (Character user in RoleManager.QueryRoleByType<Character>())
                        if (!m_users.TryGetValue(user.Identity, out QuizUser res))
                            Enter(user);
                        else
                            res.Canceled = false;

                    await RoleManager.BroadcastMsgAsync(new MsgQuiz
                    {
                        Action = MsgQuiz<Client>.QuizAction.Start,
                        Param1 = (ushort) (60 - DateTime.Now.Second),
                        Param2 = MAX_QUESTION,
                        Param3 = TIME_PER_QUESTION,
                        Param4 = m_expReward[0],
                        Param5 = m_expReward[1],
                        Param6 = m_expReward[2]
                    }).ConfigureAwait(false);

                    return;
                }

                if (m_owner.Data0 == 4) // start
                {
                    Status = QuizStatus.Running;
                    m_questionIdx = -1;
                }
            }
            else
            {
                if (m_questionTimer.ToNextTime(TIME_PER_QUESTION) && ++m_questionIdx < m_quizQuestions.Count)
                {
                    DbQuiz question = m_quizQuestions[m_questionIdx];
                    foreach (QuizUser player in m_users.Values.Where(x => !x.Canceled))
                    {
                        Character user = RoleManager.GetUser(player.Identity);
                        if (user == null)
                            continue;

                        if (!player.Replied)
                        {
                            player.Points += 1;
                            player.TimeTaken += TIME_PER_QUESTION;
                        }

                        player.Replied = false;
                        player.CurrentQuestion = m_questionIdx;
                        ushort lastResult = 1;
                        if (m_questionIdx > 0)
                            lastResult = (ushort) (player.Correct ? 1 : 2);
                        player.Correct = false;
                        _ = user.SendAsync(new MsgQuiz
                        {
                            Action = MsgQuiz<Client>.QuizAction.Question,
                            Param1 = (ushort) (m_questionIdx + 1),
                            Param2 = lastResult,
                            Param3 = player.Experience,
                            Param4 = player.TimeTaken,
                            Param5 = player.Points,
                            Strings =
                            {
                                question.Question,
                                question.Answer1,
                                question.Answer2,
                                question.Answer3,
                                question.Answer4
                            }
                        }).ConfigureAwait(false);
                    }

                    m_lastCorrectReply = question.Result;
                }
                else if (m_questionIdx >= m_quizQuestions.Count)
                {
                    Status = QuizStatus.Idle;

                    List<QuizUser> top3 = GetTop3();
                    foreach (QuizUser player in m_users.Values.Where(x => !x.Canceled))
                    {
                        if (player.CurrentQuestion < m_questionIdx)
                            player.TimeTaken += TIME_PER_QUESTION;

                        var expBallReward = 0;
                        if (top3.Any(x => x.Identity == player.Identity))
                        {
                            int rank = GetRanking(player.Identity);
                            if (rank > 0 && rank <= 3) expBallReward = m_expReward[rank];
                        }
                        else
                        {
                            expBallReward = m_expReward[3];
                        }

                        Character user = RoleManager.GetUser(player.Identity);
                        if (user != null)
                        {
                            var msg = new MsgQuiz
                            {
                                Action = MsgQuiz<Client>.QuizAction.Finish,
                                Param1 = player.Rank,
                                Param2 = player.Experience,
                                Param3 = player.TimeTaken,
                                Param4 = player.Points
                            };
                            foreach (QuizUser top in top3)
                                msg.Scores.Add(new MsgQuiz<Client>.QuizRank
                                {
                                    Name = top.Name,
                                    Time = top.TimeTaken,
                                    Score = top.Points
                                });
                            await user.SendAsync(msg);

                            if (user.Level < Role.MAX_UPLEV)
                                await user.AwardExperienceAsync(user.CalculateExpBall(expBallReward));
                        }
                        else
                        {
                            DbCharacter dbUser = await CharactersRepository.FindByIdentityAsync(player.Identity);
                            if (dbUser != null && dbUser.Level < Role.MAX_UPLEV)
                            {
                                user = new Character(dbUser, null);
                                dbUser.Experience += (ulong) user.CalculateExpBall(expBallReward);
                                await ServerDbContext.SaveAsync(dbUser);
                            }
                        }
                    }
                }
            }
        }

        #region Reply

        public async Task OnReplyAsync(Character user, ushort idxQuestion, ushort reply)
        {
            if (Status != QuizStatus.Running)
                return;

            if (!m_users.TryGetValue(user.Identity, out QuizUser player))
                m_users.TryAdd(user.Identity, player = new QuizUser
                {
                    Identity = user.Identity,
                    Name = user.Name,
                    TimeTaken = (ushort) (Math.Max(0, m_questionIdx - 1) * TIME_PER_QUESTION),
                    CurrentQuestion = m_questionIdx
                });

            if (player.CurrentQuestion != m_questionIdx)
                return;

            DbQuiz question = m_quizQuestions[idxQuestion - 1];
            ushort points;
            int expBallAmount;
            if (question.Result == reply)
            {
                expBallAmount = TOTAL_EXP_REWARD / MAX_QUESTION;
                player.Points += points = (ushort) Math.Max(1, m_questionTimer.GetRemain());
                player.TimeTaken +=
                    (ushort) Math.Max(
                        1, Math.Min(TIME_PER_QUESTION, m_questionTimer.GetInterval() - m_questionTimer.GetRemain()));
                player.Correct = true;
            }
            else
            {
                expBallAmount = TOTAL_EXP_REWARD / MAX_QUESTION * 4;
                player.Points += points = 1;
                player.TimeTaken += TIME_PER_QUESTION;
                player.Correct = false;
            }

            player.Replied = true;
            user.QuizPoints += points;
            player.Experience += (ushort) expBallAmount;
            await user.AwardExperienceAsync(user.CalculateExpBall(expBallAmount));

            var msg = new MsgQuiz
            {
                Action = MsgQuiz<Client>.QuizAction.AfterReply,
                Param2 = player.TimeTaken,
                Param3 = player.Rank = GetRanking(player.Identity),
                Param6 = player.Points
            };
            List<QuizUser> top3 = GetTop3();
            foreach (QuizUser top in top3)
                msg.Scores.Add(new MsgQuiz<Client>.QuizRank
                {
                    Name = top.Name,
                    Time = top.TimeTaken,
                    Score = top.Points
                });
            await user.SendAsync(msg);
        }

        #endregion

        #region Player

        public bool Enter(Character user)
        {
            return m_users.TryAdd(user.Identity, new QuizUser
            {
                Identity = user.Identity,
                Name = user.Name
            });
        }

        public ushort GetRanking(uint idUser)
        {
            ushort pos = 1;
            foreach (QuizUser player in m_users.Values
                                               .Where(x => !x.Canceled)
                                               .OrderByDescending(x => x.Points)
                                               .ThenBy(x => x.TimeTaken))
            {
                if (player.Identity == idUser)
                    return pos;
                pos++;
            }

            return pos;
        }

        private List<QuizUser> GetTop3()
        {
            var rank = new List<QuizUser>();
            foreach (QuizUser player in m_users.Values
                                               .Where(x => !x.Canceled)
                                               .OrderByDescending(x => x.Points)
                                               .ThenBy(x => x.TimeTaken))
            {
                if (rank.Count == 3)
                    break;
                rank.Add(player);
            }

            return rank;
        }

        #endregion

        #region Broadcast

        public async Task BroadcastMsgAsync(IPacket msg)
        {
            foreach (QuizUser user in m_users.Values.Where(x => !x.Canceled))
            {
                Character player = RoleManager.GetUser(user.Identity);
                if (player == null)
                    continue;
                await player.SendAsync(msg);
            }
        }

        #endregion

        #region Cancelation

        public void Cancel(uint idUser)
        {
            if (m_users.TryGetValue(idUser, out QuizUser value)) value.Canceled = true;
        }

        public bool IsCanceled(uint idUser)
        {
            return m_users.TryGetValue(idUser, out QuizUser value) && value.Canceled;
        }

        #endregion

        public enum QuizStatus
        {
            Idle,
            Running
        }

        private class QuizUser
        {
            public uint Identity { get; set; }
            public string Name { get; set; }
            public ushort Points { get; set; }
            public ushort Experience { get; set; } // 600 = 1 expball
            public ushort TimeTaken { get; set; }  // in seconds
            public int CurrentQuestion { get; set; }
            public ushort Rank { get; set; }
            public bool Correct { get; set; }
            public bool Replied { get; set; }
            public bool Canceled { get; set; }
        }
    }
}