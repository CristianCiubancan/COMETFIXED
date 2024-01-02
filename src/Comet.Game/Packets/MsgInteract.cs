using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;
using Org.BouncyCastle.Asn1.X509;
using static Comet.Network.Packets.Game.MsgName<Comet.Game.States.Client>;

namespace Comet.Game.Packets
{
    public sealed class MsgInteract : MsgInteract<Client>
    {
        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            Role sender = RoleManager.GetRole(SenderIdentity);

            if (SenderIdentity == user.Identity && !user.IsAlive)
            {
                await user.SendAsync(Language.StrDead);
                return;
            }

            if (sender != null 
                && (sender.QueryStatus(StatusSet.FREEZE) != null
                || sender.QueryStatus(StatusSet.ICE_BLOCK) != null
                || sender.QueryStatus(StatusSet.DAZED) != null
                || sender.QueryStatus(StatusSet.HUGE_DAZED) != null
                || sender.QueryStatus(StatusSet.CONFUSED) != null))
                return;

            switch (Action)
            {
                case MsgInteractType.Attack:
                case MsgInteractType.Shoot:
                case MsgInteractType.MagicAttack:
                case MsgInteractType.AbortMagic:
                case MsgInteractType.Court:
                case MsgInteractType.Marry:
                {
                    sender?.BattleSystem.ResetBattle();
                    if (sender?.MagicData.QueryMagic != null)
                        await sender.MagicData.AbortMagicAsync(true);
                    break;
                }
            }

            Role target = sender?.Map.QueryAroundRole(sender, TargetIdentity);

            if (target == null 
                && Action != MsgInteractType.MagicAttack
                && Action != MsgInteractType.CounterKillSwitch)
                return;

            switch (Action)
            {
                case MsgInteractType.Attack:
                case MsgInteractType.Shoot:
                {
                    if (sender?.IsAlive != true)
                        return;

                    if (user.Identity == sender.Identity)
                    {
                        if (user.QueryStatus(StatusSet.FATAL_STRIKE) == null && !await user.SynPositionAsync(PosX, PosY, 8))
                        {
                            await RoleManager.KickOutAsync(sender.Identity, "MsgInteract SynPosition");
                            return;
                        }
                    }

                    if (sender.SetAttackTarget(target))
                        sender.BattleSystem.CreateBattle(TargetIdentity);
                    break;
                }

                case MsgInteractType.MagicAttack:
                {
                    if (sender?.IsAlive != true)
                        return;

                    byte[] dataArray = BitConverter.GetBytes(Data);
                    Data = Convert.ToUInt16((dataArray[0] & 0xFF) | ((dataArray[1] & 0xFF) << 8));
                    Data ^= 0x915d;
                    Data ^= (ushort)client.Identity;
                    Data = (ushort)((Data << 0x3) | (Data >> 0xd));
                    Data -= 0xeb42;

                    dataArray = BitConverter.GetBytes(TargetIdentity);
                    TargetIdentity = ((uint)dataArray[0] & 0xFF) | (((uint)dataArray[1] & 0xFF) << 8) |
                                     (((uint)dataArray[2] & 0xFF) << 16) | (((uint)dataArray[3] & 0xFF) << 24);
                    TargetIdentity = ((((TargetIdentity & 0xffffe000) >> 13) | ((TargetIdentity & 0x1fff) << 19)) ^
                                      0x5F2D2463 ^ client.Identity) -
                                     0x746F4AE6;

                    dataArray = BitConverter.GetBytes(PosX);
                    long xx = (dataArray[0] & 0xFF) | ((dataArray[1] & 0xFF) << 8);
                    dataArray = BitConverter.GetBytes(PosY);
                    long yy = (dataArray[0] & 0xFF) | ((dataArray[1] & 0xFF) << 8);
                    xx = xx ^ (client.Identity & 0xffff) ^ 0x2ed6;
                    xx = ((xx << 1) | ((xx & 0x8000) >> 15)) & 0xffff;
                    xx |= 0xffff0000;
                    xx -= 0xffff22ee;
                    yy = yy ^ (client.Identity & 0xffff) ^ 0xb99b;
                    yy = ((yy << 5) | ((yy & 0xF800) >> 11)) & 0xffff;
                    yy |= 0xffff0000;
                    yy -= 0xffff8922;
                    PosX = Convert.ToUInt16(xx);
                    PosY = Convert.ToUInt16(yy);

                    await sender.ProcessMagicAttackAsync((ushort) Data, TargetIdentity, PosX, PosY);
                    break;
                }

                case MsgInteractType.Chop:
                {
                    Item targetItem = user?.UserPackage.GetItemByType(Item.TYPE_JAR);
                    if (targetItem == null)
                        return;

                    Command = (ushort) (targetItem.Data * 2);
                    await client.SendAsync(this);

                    break;
                }

                case MsgInteractType.Court:
                {
                    if (target == null || target.Identity == user.Identity)
                        return;

                    if (target is not Character targetUser)
                        return;

                    if (targetUser.MapIdentity != user.MapIdentity || user.GetDistance(targetUser) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTargetNotInRange);
                        return;
                    }

                    if (targetUser.MateIdentity != 0)
                    {
                        await user.SendAsync(Language.StrMarriageTargetNotSingle);
                        return; // target is already married
                    }

                    if (user.MateIdentity != 0)
                    {
                        await user.SendAsync(Language.StrMarriageYouNoSingle);
                        return; // you're already married
                    }

                    if (user.Gender == targetUser.Gender)
                    {
                        await user.SendAsync(Language.StrMarriageErrSameGender);
                        return; // not allow same gender
                    }

                    targetUser.SetRequest(RequestType.Marriage, user.Identity);
                    await targetUser.SendAsync(this);
                    break;
                }

                case MsgInteractType.Marry:
                {
                    if (target == null || target.Identity == user.Identity)
                        return;

                    if (!(target is Character targetUser))
                        return;

                    if (user.QueryRequest(RequestType.Marriage) != targetUser.Identity)
                    {
                        await user.SendAsync(Language.StrMarriageNotApply);
                        return;
                    }

                    user.PopRequest(RequestType.Marriage);

                    if (targetUser.MapIdentity != user.MapIdentity || user.GetDistance(targetUser) > Screen.VIEW_SIZE)
                    {
                        await user.SendAsync(Language.StrTargetNotInRange);
                        return;
                    }

                    if (targetUser.MateIdentity != 0)
                    {
                        await user.SendAsync(Language.StrMarriageTargetNotSingle);
                        return; // target is already married
                    }

                    if (user.MateIdentity != 0)
                    {
                        await user.SendAsync(Language.StrMarriageYouNoSingle);
                        return; // you're already married
                    }

                    if (user.Gender == targetUser.Gender)
                    {
                        await user.SendAsync(Language.StrMarriageErrSameGender);
                        return; // not allow same gender
                    }

                    user.MateIdentity = targetUser.Identity;
                    user.MateName = targetUser.Name;
                    await user.SaveAsync();
                    targetUser.MateIdentity = user.Identity;
                    targetUser.MateName = user.Name;
                    await targetUser.SaveAsync();

                    await user.SendAsync(new MsgName
                    {
                        Identity = user.Identity,
                        Strings = new List<string> {targetUser.Name},
                        Action = StringAction.Mate
                    });

                    await targetUser.SendAsync(new MsgName
                    {
                        Identity = targetUser.Identity,
                        Strings = new List<string> {user.Name},
                        Action = StringAction.Mate
                    });

                    await user.BroadcastRoomMsgAsync(new MsgItem
                    {
                        Action = MsgItem<Client>.ItemActionType.Fireworks,
                        Identity = user.Identity
                    }, false);

                    await targetUser.BroadcastRoomMsgAsync(new MsgItem
                    {
                        Action = MsgItem<Client>.ItemActionType.Fireworks,
                        Identity = targetUser.Identity
                    }, false);
                    await RoleManager.BroadcastMsgAsync(string.Format(Language.StrMarry, targetUser.Name, user.Name),
                                                        TalkChannel.Center, Color.Red);
                    break;
                }

                case MsgInteractType.InitialMerchant:
                case MsgInteractType.AcceptMerchant:
                {
                    // ON ACCEPT: Sender = 1 Target = 1
                    if (SenderIdentity == 1 && TargetIdentity == 1)
                        await user.SetMerchantAsync();
                    break;
                }

                case MsgInteractType.CancelMerchant:
                {
                    await user.RemoveMerchantAsync();
                    break;
                }

                case MsgInteractType.MerchantProgress:
                {
                    break;
                }

                case MsgInteractType.CounterKillSwitch:
                {
                    if (user.MagicData[6003] == null) // must have skill
                        return;

                    if (!user.IsAlive || user.IsWing)
                        return;

                    await user.SetScapegoatAsync(!user.Scapegoat);
                    break;
                }

                case MsgInteractType.CoupleActionRequest:
                {
                    Character userTarget = RoleManager.GetUser(TargetIdentity);
                    if (userTarget == null)
                        return;

                    if (user.GetDistance(userTarget) >= Screen.VIEW_SIZE)
                        return;

                    if (user.HasCoupleInteraction() || user.HasCoupleInteractionStarted())
                        return;

                    if (userTarget.HasCoupleInteraction() || userTarget.HasCoupleInteractionStarted())
                        return;

                    userTarget.SetRequest(RequestType.CoupleInteraction, user.Identity, Data);
                    await userTarget.SendAsync(this);
                    break;
                }

                case MsgInteractType.CoupleActionConfirm:
                {
                    uint targetId = user.QueryRequest(RequestType.CoupleInteraction);
                    if (targetId != TargetIdentity)
                        return;

                    Character targetUser = RoleManager.GetUser(targetId);
                    if (targetUser == null)
                        return;

                    int requiredAction = user.QueryRequestData(RequestType.CoupleInteraction);
                    if (!Enum.IsDefined(typeof(EntityAction), (ushort) requiredAction))
                        return;

                    if (user.IsWing || targetUser.IsWing)
                        return;

                    if (user.HasCoupleInteraction() || user.HasCoupleInteractionStarted())
                        return;

                    if (targetUser.HasCoupleInteraction() || targetUser.HasCoupleInteractionStarted())
                        return;

                    await targetUser.SendAsync(this);

                    Timestamp = 0;
                    await user.SendAsync(this);

                    await user.SetActionAsync((EntityAction) requiredAction, targetUser.Identity);
                    await targetUser.SetActionAsync((EntityAction) requiredAction, user.Identity);

                    user.PopRequest(RequestType.CoupleInteraction);
                    break;
                }

                case MsgInteractType.CoupleActionRefuse:
                {
                    uint targetId = user.PopRequest(RequestType.CoupleInteraction);
                    if (targetId != TargetIdentity)
                        return;

                    Character targetUser = RoleManager.GetUser(targetId);
                    if (targetUser == null)
                        return;

                    await targetUser.SendAsync(this);
                    break;
                }

                case MsgInteractType.CoupleActionStart:
                {
                    if (!user.HasCoupleInteraction() || user.HasCoupleInteractionStarted())
                        return;

                    Character couple = user.GetCoupleInteractionTarget();
                    if (couple == null)
                    {
                        user.CancelCoupleInteraction();
                        return;
                    }

                    if (user.GetDistance(couple) > 1)
                    {
                        couple.CancelCoupleInteraction();
                        user.CancelCoupleInteraction();
                        return;
                    }

                    user.StartCoupleInteraction();
                    couple.StartCoupleInteraction();

                    (SenderIdentity, TargetIdentity) = (TargetIdentity, SenderIdentity);
                    await user.BroadcastRoomMsgAsync(this, true);

                    (SenderIdentity, TargetIdentity) = (TargetIdentity, SenderIdentity);
                    await couple.BroadcastRoomMsgAsync(this, true);
                    break;
                }

                case MsgInteractType.CoupleActionEnd:
                {
                    await user.BroadcastRoomMsgAsync(this, true);
                    (TargetIdentity, SenderIdentity) = (SenderIdentity, TargetIdentity);
                    await user.BroadcastRoomMsgAsync(this, true);

                    user.CancelCoupleInteraction();

                    Character couple = RoleManager.GetUser(SenderIdentity);
                    if (couple != null) couple.CancelCoupleInteraction();
                    break;
                }

                default:
                {
                    await client.SendAsync(new MsgTalk(client.Identity, TalkChannel.Service,
                                                           $"Missing packet {Type}, Action {Action}, Length {Length}"));
                    await Log.WriteLogAsync(LogLevel.Warning,
                                            "Missing packet {0}, Action {1}, Length {2}\n{3}",
                                            Type, Action, Length, PacketDump.Hex(Encode()));
                    break;
                }
            }
        }
    }
}