using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.States;
using Comet.Game.States.Guide;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgGuide : MsgGuide<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Action)
            {
                case Request.InviteApprentice:
                {
                    Character target = RoleManager.GetUser(Param);
                    if (target == null)
                        return;

                    if (user.Level < target.Level || user.Metempsychosis < target.Metempsychosis)
                    {
                        await user.SendAsync(Language.StrGuideStudentHighLevel);
                        return;
                    }

                    int deltaLevel = user.Level - target.Level;
                    if (target.Metempsychosis == 0)
                    {
                        if (deltaLevel > 30)
                        {
                            await user.SendAsync(Language.StrGuideStudentHighLevel);
                            return;
                        }
                    }
                    else if (target.Metempsychosis == 1)
                    {
                        if (deltaLevel > 20)
                        {
                            await user.SendAsync(Language.StrGuideStudentHighLevel);
                            return;
                        }
                    }
                    else
                    {
                        if (deltaLevel < 10)
                        {
                            await user.SendAsync(Language.StrGuideStudentHighLevel);
                            return;
                        }
                    }

                    DbTutorType type = TutorManager.GetTutorType(user.Level);
                    if (type == null || user.ApprenticeCount >= type.StudentNum)
                    {
                        await user.SendAsync(Language.StrGuideTooManyStudents);
                        return;
                    }

                    target.SetRequest(RequestType.Guide, user.Identity);

                    await target.SendAsync(new MsgGuide
                    {
                        Identity = user.Identity,
                        Param = user.Identity,
                        Param2 = (uint) user.BattlePower,
                        Action = Request.AcceptRequestApprentice,
                        Online = true,
                        Name = user.Name
                    });
                    await target.SendRelationAsync(user);

                    await user.SendAsync(Language.StrGuideSendTutor);
                    break;
                }

                case Request.RequestMentor:
                {
                    Character target = RoleManager.GetUser(Param);
                    if (target == null)
                        return;

                    if (target.Level < user.Level || target.Metempsychosis < user.Metempsychosis)
                    {
                        await user.SendAsync(Language.StrGuideStudentHighLevel1);
                        return;
                    }

                    int deltaLevel = target.Level - user.Level;
                    if (target.Metempsychosis == 0)
                    {
                        if (deltaLevel < 30)
                        {
                            await user.SendAsync(Language.StrGuideStudentHighLevel1);
                            return;
                        }
                    }
                    else if (target.Metempsychosis == 1)
                    {
                        if (deltaLevel > 20)
                        {
                            await user.SendAsync(Language.StrGuideStudentHighLevel1);
                            return;
                        }
                    }
                    else
                    {
                        if (deltaLevel < 10)
                        {
                            await user.SendAsync(Language.StrGuideStudentHighLevel1);
                            return;
                        }
                    }

                    DbTutorType type = TutorManager.GetTutorType(target.Level);
                    if (type == null || target.ApprenticeCount >= type.StudentNum)
                    {
                        await user.SendAsync(Language.StrGuideTooManyStudents1);
                        return;
                    }

                    target.SetRequest(RequestType.Guide, user.Identity);

                    await target.SendAsync(new MsgGuide
                    {
                        Identity = user.Identity,
                        Param = user.Identity,
                        Param2 = (uint) user.BattlePower,
                        Action = Request.AcceptRequestApprentice,
                        Online = true,
                        Name = user.Name
                    });
                    await target.SendRelationAsync(user);

                    await user.SendAsync(Language.StrGuideSendTutor);
                    break;
                }

                case Request.LeaveMentor:
                {
                    if (user.Guide == null)
                        return;

                    if (user.Guide.Betrayed)
                        return;

                    if (!await user.SpendMoneyAsync(Tutor.STUDENT_BETRAYAL_VALUE, true))
                        return;

                    await user.Guide.BetrayAsync();

                    Character guide = user.Guide.Guide;
                    if (guide != null) await guide.SendAsync(string.Format(Language.StrGuideBetrayTutor, user.Name));

                    await user.Guide.SendTutorAsync();
                    break;
                }

                case Request.DumpApprentice:
                {
                    if (!user.IsApprentice(Identity))
                        return;

                    Tutor tutor = user.GetStudent(Identity);
                    if (tutor == null)
                        return;

                    if (tutor.Betrayed)
                        return; // already dumped :]

                    await tutor.BetrayAsync();

                    Character student = tutor.Student;
                    if (student != null)
                        await student.SendAsync(string.Format(Language.StrGuideBetray, tutor.GuideName));

                    await tutor.SendStudentAsync();
                    break;
                }

                case Request.AcceptRequestApprentice:
                {
                    Character target = RoleManager.GetUser(Identity);
                    if (target == null)
                        return;

                    if (Param2 == 0)
                    {
                        await target.SendAsync(Language.StrGuideDeclined);
                        return;
                    }

                    if (user.QueryRequest(RequestType.Guide) == Identity)
                    {
                        user.PopRequest(RequestType.Guide);
                        await Character.CreateTutorRelationAsync(user, target);
                    }

                    break;
                }

                case Request.AcceptRequestMentor:
                {
                    if (Param2 == 0)
                    {
                        await user.SendAsync(Language.StrGuideDeclined);
                        return;
                    }

                    Character target = RoleManager.GetUser(Identity);
                    if (target == null)
                        return;

                    if (user.QueryRequest(RequestType.Guide) == Identity)
                    {
                        user.PopRequest(RequestType.Guide);
                        await Character.CreateTutorRelationAsync(target, user);
                    }

                    break;
                }

                default:
                    if (user.IsPm())
                        await user.SendAsync($"Unhandled MsgGuide:{Action}", TalkChannel.Talk);
                    break;
            }
        }
    }
}