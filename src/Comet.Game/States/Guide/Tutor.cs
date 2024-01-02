using System;
using System.Collections.Generic;
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

namespace Comet.Game.States.Guide
{
    public sealed class Tutor
    {
        public const int BETRAYAL_FLAG_TIMEOUT = 60 * 60 * 24 * 3;
        public const int STUDENT_BETRAYAL_VALUE = 50000;

        private readonly TimeOut m_betrayCheck = new();

        private DbTutor m_tutor;
        private DbTutorContribution m_access;

        private Tutor()
        {
        }

        public static async Task<Tutor> CreateAsync(DbTutor tutor)
        {
            var guide = new Tutor
            {
                m_tutor = tutor,
                m_access = await TutorContributionRepository.GetGuideAsync(tutor.StudentId)
            };
            guide.m_access ??= new DbTutorContribution
            {
                TutorIdentity = tutor.GuideId,
                StudentIdentity = tutor.StudentId
            };

            DbCharacter dbMentor = await CharactersRepository.FindByIdentityAsync(tutor.GuideId);
            if (dbMentor == null)
                return null;
            guide.GuideName = dbMentor.Name;

            dbMentor = await CharactersRepository.FindByIdentityAsync(tutor.StudentId);
            if (dbMentor == null)
                return null;
            guide.StudentName = dbMentor.Name;

            if (guide.Betrayed)
                guide.m_betrayCheck.Startup(60);

            return guide;
        }

        public uint GuideIdentity => m_tutor.GuideId;
        public string GuideName { get; private set; }

        public uint StudentIdentity => m_tutor.StudentId;
        public string StudentName { get; private set; }

        public bool Betrayed => m_tutor.BetrayalFlag != 0;
        public bool BetrayalCheck => Betrayed && m_betrayCheck.IsActive() && m_betrayCheck.ToNextTime();

        public Character Guide => RoleManager.GetUser(m_tutor.GuideId);
        public Character Student => RoleManager.GetUser(m_tutor.StudentId);

        public async Task<bool> AwardTutorExperienceAsync(uint addExpTime)
        {
            m_access.Experience += addExpTime;

            Character user = RoleManager.GetUser(m_access.TutorIdentity);
            if (user != null)
            {
                user.MentorExpTime += addExpTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await TutorAccessRepository.GetAsync(m_access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Experience += addExpTime;
                await ServerDbContext.SaveAsync(tutorAccess);
            }

            return await SaveAsync();
        }

        public async Task<bool> AwardTutorGodTimeAsync(ushort addGodTime)
        {
            m_access.GodTime += addGodTime;

            Character user = RoleManager.GetUser(m_access.TutorIdentity);
            if (user != null)
            {
                user.MentorGodTime += addGodTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await TutorAccessRepository.GetAsync(m_access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Blessing += addGodTime;
                await ServerDbContext.SaveAsync(tutorAccess);
            }

            return await SaveAsync();
        }

        public async Task<bool> AwardOpportunityAsync(ushort addTime)
        {
            m_access.PlusStone += addTime;

            Character user = RoleManager.GetUser(m_access.TutorIdentity);
            if (user != null)
            {
                user.MentorAddLevexp += addTime;
            }
            else
            {
                DbTutorAccess tutorAccess = await TutorAccessRepository.GetAsync(m_access.TutorIdentity);
                tutorAccess ??= new DbTutorAccess
                {
                    GuideIdentity = GuideIdentity
                };
                tutorAccess.Composition += addTime;
                await ServerDbContext.SaveAsync(tutorAccess);
            }

            return await SaveAsync();
        }

        public int SharedBattlePower
        {
            get
            {
                Character mentor = Guide;
                Character student = Student;
                if (mentor == null || student == null)
                    return 0;
                if (mentor.PureBattlePower < student.PureBattlePower)
                    return 0;

                DbTutorBattleLimitType limit = TutorManager.GetTutorBattleLimitType(student.PureBattlePower);
                if (limit == null)
                    return 0;

                DbTutorType type = TutorManager.GetTutorType(mentor.Level);
                if (type == null)
                    return 0;

                return (int) Math.Min(limit.BattleLevelLimit,
                                      (mentor.PureBattlePower - student.PureBattlePower) *
                                      (type.BattleLevelShare / 100f));
            }
        }

        public async Task BetrayAsync()
        {
            m_tutor.BetrayalFlag = UnixTimestamp.Now();
            await SaveAsync();
        }

        public async Task SendTutorAsync()
        {
            if (Student == null)
                return;

            await Student.SendAsync(new MsgGuideInfo
            {
                Identity = StudentIdentity,
                Level = Guide?.Level ?? 0,
                Blessing = m_access.GodTime,
                Composition = (ushort) m_access.PlusStone,
                Experience = m_access.Experience,
                IsOnline = Guide != null,
                Mesh = Guide?.Mesh ?? 0,
                Mode = MsgGuideInfo<Client>.RequestMode.Mentor,
                Syndicate = Guide?.SyndicateIdentity ?? 0,
                SyndicatePosition = (ushort) (Guide?.SyndicateRank ?? SyndicateMember.SyndicateRank.None),
                Names = new List<string>
                {
                    GuideName,
                    StudentName,
                    Guide?.MateName ?? Language.StrNone
                },
                EnroleDate = uint.Parse(m_tutor.Date?.ToString("yyyyMMdd") ?? "0"),
                PkPoints = Guide?.PkPoints ?? 0,
                Profession = Guide?.Profession ?? 0,
                SharedBattlePower = (uint) SharedBattlePower,
                SenderIdentity = GuideIdentity,
                Unknown24 = 999999
            });
        }

        public async Task SendStudentAsync()
        {
            if (Guide == null)
                return;

            await Guide.SendAsync(new MsgGuideInfo
            {
                Identity = StudentIdentity,
                Level = Student?.Level ?? 0,
                Blessing = m_access.GodTime,
                Composition = (ushort) m_access.PlusStone,
                Experience = m_access.Experience,
                IsOnline = Student != null,
                Mesh = Student?.Mesh ?? 0,
                Mode = MsgGuideInfo<Client>.RequestMode.Apprentice,
                Syndicate = Student?.SyndicateIdentity ?? 0,
                SyndicatePosition = (ushort) (Student?.SyndicateRank ?? SyndicateMember.SyndicateRank.None),
                Names = new List<string>
                {
                    GuideName,
                    StudentName,
                    Student?.MateName ?? Language.StrNone
                },
                EnroleDate = uint.Parse(m_tutor.Date?.ToString("yyyyMMdd") ?? "0"),
                PkPoints = Student?.PkPoints ?? 0,
                Profession = Student?.Profession ?? 0,
                SharedBattlePower = 0,
                SenderIdentity = GuideIdentity,
                Unknown24 = 999999
            });
        }

        public async Task BetrayalTimerAsync()
        {
            /*
             * Since this will be called in a queue, it might be called twice per run, so we will trigger the TimeOut
             * to see it can be checked.
             */
            if (m_tutor.BetrayalFlag != 0)
                if (m_tutor.BetrayalFlag + BETRAYAL_FLAG_TIMEOUT < UnixTimestamp.Now()) // expired, leave mentor
                {
                    if (Guide != null)
                    {
                        await Guide.SendAsync(string.Format(Language.StrGuideExpelTutor, StudentName));
                        Guide.RemoveApprentice(StudentIdentity);
                    }

                    if (Student != null)
                    {
                        await Student.SendAsync(string.Format(Language.StrGuideExpelStudent, GuideName));
                        await Student.SynchroAttributesAsync(ClientUpdateType.ExtraBattlePower, 0, 0);
                        Student.Guide = null;
                    }

                    await DeleteAsync();
                }
        }

        public async Task<bool> SaveAsync()
        {
            return await ServerDbContext.SaveAsync(m_tutor) && await ServerDbContext.SaveAsync(m_access);
        }

        public async Task<bool> DeleteAsync()
        {
            await ServerDbContext.DeleteAsync(m_tutor);
            await ServerDbContext.DeleteAsync(m_access);
            return true;
        }
    }
}