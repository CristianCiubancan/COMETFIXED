using System;
using System.Drawing;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.Packets.Ai;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Game.States.Items;
using Comet.Game.States.Relationship;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1010</remarks>
    /// <summary>
    ///     Message containing a general action being performed by the client. Commonly used
    ///     as a request-response protocol for question and answer like exchanges. For example,
    ///     walk requests are responded to with an answer as to if the step is legal or not.
    /// </summary>
    public sealed class MsgAction : MsgAction<Client>
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
            Role role = RoleManager.GetRole(Identity);
            Character targetUser = RoleManager.GetUser(Command);

            switch (Action)
            {
                case ActionType.CharacterDirection:   // 79
                case ActionType.CharacterEmote:       // 81
                case ActionType.CharacterObservation: // 117
                case ActionType.FriendObservation:    // 310
                {
                    if (user.Identity == Identity)
                    {
                        user?.BattleSystem.ResetBattle();
                        await user.MagicData.AbortMagicAsync(true);
                    }

                    break;
                }
            }

            //Console.WriteLine($"Action {Action} => user: {user.Identity} target: {Identity}");

            switch (Action)
            {
                case ActionType.LoginSpawn: // 74
                {
                    if (user == null)
                        return;

                    Identity = client.Character.Identity;

                    if (user.IsOfflineTraining)
                    {
                        client.Character.MapIdentity = 601;
                        client.Character.MapX = 61;
                        client.Character.MapY = 54;
                    }

                    GameMap targetMap = MapManager.GetMap(client.Character.MapIdentity);
                    if (targetMap == null)
                    {
                        await user.SavePositionAsync(1002, 430, 378);
                        client.Disconnect();
                        return;
                    }

                    Command = targetMap.MapDoc;
                    X = client.Character.MapX;
                    Y = client.Character.MapY;

                    await client.Character.EnterMapAsync();
                    await client.SendAsync(this);

                    await GameAction.ExecuteActionAsync(1000000, user, null, null, "");

                    if (user.Life == 0)
                        await user.SetAttributesAsync(ClientUpdateType.Hitpoints, 10);

                    await Kernel.BroadcastWorldMsgAsync(new MsgAiPlayerLogin(user));
                    break;
                }

                case ActionType.LoginInventory: // 75
                {
                    if (user == null)
                        return;

                    await user.UserPackage.CreateAsync();
                    await user.UserPackage.SendAsync();
                    await user.SendDetainRewardAsync();
                    await user.SendDetainedEquipmentAsync();
                    await user.LoadTitlesAsync();
                    await client.SendAsync(this);
                    break;
                }

                case ActionType.LoginRelationships: // 76
                {
                    if (user == null)
                        return;

                    foreach (DbFriend dbFriend in await FriendRepository.GetAsync(user.Identity))
                    {
                        var friend = new Friend(user);
                        await friend.CreateAsync(dbFriend);
                        user.AddFriend(friend);
                    }

                    await user.SendAllFriendAsync();

                    foreach (DbEnemy dbEnemy in await EnemyRepository.GetAsync(user.Identity))
                    {
                        if (dbEnemy.TargetIdentity == user.Identity)
                        {
                            await ServerDbContext.DeleteAsync(dbEnemy);
                            continue;
                        }

                        var enemy = new Enemy(user);
                        await enemy.CreateAsync(dbEnemy);
                        user.AddEnemy(enemy);
                    }

                    await user.SendAllEnemiesAsync();

                    if (user.MateIdentity != 0)
                    {
                        Character mate = RoleManager.GetUser(user.MateIdentity);
                        if (mate != null)
                            await mate.SendAsync(user.Gender == 1
                                                     ? Language.StrMaleMateLogin
                                                     : Language.StrFemaleMateLogin);
                    }

                    await user.LoadGuideAsync();
                    await user.LoadTradePartnerAsync();
                    await user.LoadMonsterKillsAsync();

                    await client.SendAsync(this);
                    break;
                }

                case ActionType.LoginProficiencies: // 77
                {
                    await client.Character.WeaponSkill.InitializeAsync();
                    await client.Character.WeaponSkill.SendAsync();
                    await client.SendAsync(this);
                    break;
                }

                case ActionType.LoginSpells: // 78
                {
                    await client.Character.MagicData.InitializeAsync();
                    await client.Character.MagicData.SendAllAsync();
                    await client.SendAsync(this);
                    break;
                }

                case ActionType.CharacterDirection: // 79
                {
                    await client.Character.SetDirectionAsync((FacingDirection) (Direction % 8), false);
                    await client.Character.BroadcastRoomMsgAsync(this, true);
                    break;
                }

                case ActionType.CharacterEmote: // 81
                {
                    if (user != null && user.Identity == Identity)
                    {
                        await role.SetActionAsync((EntityAction) Command, false);
                        await role.BroadcastRoomMsgAsync(this, user?.Identity == Identity);
                    }
                    //else if (false)
                    //{
                    //    await user.SendAsync(this);
                    //}
                    break;
                }

                case ActionType.MapPortal: // 85
                {
                    uint idMap = 0;
                    var tgtPos = new Point();
                    var sourcePos = new Point(client.Character.MapX, client.Character.MapY);
                    if (!client.Character.Map.GetPassageMap(ref idMap, ref tgtPos, ref sourcePos))
                        client.Character.Map.GetRebornMap(ref idMap, ref tgtPos);
                    await client.Character.FlyMapAsync(idMap, tgtPos.X, tgtPos.Y);
                    break;
                }

                case ActionType.SpellAbortXp: // 93
                {
                    if (client.Character.QueryStatus(StatusSet.START_XP) != null)
                        await client.Character.DetachStatusAsync(StatusSet.START_XP);
                    break;
                }

                case ActionType.CharacterRevive: // 94
                {
                    if (user == null)
                        return;

                    if (user.IsAlive || !user.CanRevive())
                        return;

                    await user.RebornAsync(Command == 0);
                    break;
                }

                case ActionType.CharacterDelete:
                {
                    if (user == null)
                        return;

                    if (user.SecondaryPassword != Command)
                        return;

                    if (await user.DeleteCharacterAsync())
                        await RoleManager.KickOutAsync(user.Identity, "DELETED");
                    break;
                }

                case ActionType.CharacterPkMode: // 96
                {
                    if (!Enum.IsDefined(typeof(PkModeType), (int) Command))
                        Command = (uint) PkModeType.Capture;

                    client.Character.PkMode = (PkModeType) Command;
                    await client.SendAsync(this);
                    break;
                }

                case ActionType.LoginGuild: // 97
                {
                    client.Character.Syndicate = SyndicateManager.FindByUser(client.Identity);
                    await client.Character.SendSyndicateAsync();
                    if (client.Character.Syndicate != null)
                        await client.Character.Syndicate.SendRelationAsync(client.Character);

                    await client.Character.LoadFamilyAsync();
                    await client.SendAsync(this);
                    break;
                }

                case ActionType.MapMine:
                {
                    if (user == null)
                        return;

                    if (!user.IsAlive)
                    {
                        await user.SendAsync(Language.StrDead);
                        return;
                    }

                    if (!user.Map.IsMineField())
                    {
                        await user.SendAsync(Language.StrNoMine);
                        return;
                    }

                    user.StartMining();
                    break;
                }

                case ActionType.MapTeamLeaderStar: // 101
                {
                    if (user == null)
                        return;

                    if (user.Team == null || user.Team.Leader.MapIdentity != user.MapIdentity)
                        return;

                    targetUser = user.Team.Leader;
                    X = targetUser.MapX;
                    Y = targetUser.MapY;
                    await user.SendAsync(this);
                    break;
                }

                case ActionType.MapQuery: // 102
                {
                    if (targetUser != null)
                        await targetUser.SendSpawnToAsync(user);
                    break;
                }

                case ActionType.MapTeamMemberStar: // 106
                {
                    if (user == null)
                        return;

                    if (user.Team == null || targetUser == null || !user.Team.IsMember(targetUser.Identity) ||
                        targetUser.MapIdentity != user.MapIdentity)
                        return;

                    Command = targetUser.RecordMapIdentity;
                    X = targetUser.MapX;
                    Y = targetUser.MapY;
                    await user.SendAsync(this);
                    break;
                }

                case ActionType.BoothSpawn:
                {
                    if (user == null)
                        return;

                    if (await user.CreateBoothAsync())
                    {
                        Command = user.Booth.Identity;
                        X = user.Booth.MapX;
                        Y = user.Booth.MapY;
                        await user.SendAsync(this);
                    }

                    break;
                }

                case ActionType.BoothLeave: // 114
                {
                    if (user == null)
                        return;

                    await user.DestroyBoothAsync();
                    await user.Screen.SynchroScreenAsync();
                    break;
                }

                case ActionType.CharacterObservation: // 117
                {
                    if (user == null)
                        return;

                    targetUser = RoleManager.GetUser(Command);
                    if (targetUser == null)
                        return;

                    for (var pos = Item.ItemPosition.EquipmentBegin;
                         pos <= Item.ItemPosition.EquipmentEnd;
                         pos++)
                        if (targetUser.UserPackage[pos] != null)
                            await user.SendAsync(
                                new MsgItemInfo(targetUser.UserPackage[pos], MsgItemInfo<Client>.ItemMode.View));

                    await targetUser.SendAsync(string.Format(Language.StrObservingEquipment, user.Name));
                    break;
                }

                case ActionType.SpellAbortTransform: // 118
                {
                    if (user?.Transformation != null)
                        await user.ClearTransformationAsync();
                    break;
                }

                case ActionType.SpellAbortFlight: // 120
                {
                    if (user?.QueryStatus(StatusSet.FLY) != null)
                        await user.DetachStatusAsync(StatusSet.FLY);
                    break;
                }

                case ActionType.RelationshipsEnemy: // 123
                {
                    if (user == null)
                        return;

                    Enemy fetchEnemy = user.GetEnemy(Command);
                    if (fetchEnemy == null)
                    {
                        await user.SendAsync(this);
                        return;
                    }

                    await fetchEnemy.SendInfoAsync();
                    break;
                }

                case ActionType.LoginComplete: // 130
                {
                    if (user == null)
                        return;

                    int bonusCount = await user.BonusCountAsync();
                    if (bonusCount > 0)
                        await user.SendAsync(string.Format(Language.StrBonus, bonusCount), TalkChannel.Center,
                                             Color.Red);

                    if (await user.CardsCountAsync() > 0)
                        await user.SendAsync(new MsgAction
                        {
                            Action = ActionType.ClientCommand, Command = 1197u, Identity = user.Identity
                        });

                    if (user.Gender == 1 &&
                        (user.SendFlowerTime == null
                         || int.Parse(DateTime.Now.ToString("yyyyMMdd")) >
                         int.Parse(user.SendFlowerTime.Value.ToString("yyyyMMdd"))))
                        await user.SendAsync(new MsgFlower
                        {
                            Mode = MsgFlower<Client>.RequestMode.QueryIcon
                        });

                    await user.CheckPkStatusAsync();
                    await user.LoadStatusAsync();
                    await user.SendNobilityInfoAsync();
                    await user.SendMultipleExpAsync();
                    await user.SendBlessAsync();
                    await user.SendLuckAsync();
                    await user.Screen.SynchroScreenAsync();
                    await PigeonManager.SendToUserAsync(user);
                    await user.SendMerchantAsync();

                    if (user.VipLevel > 0)
                        if (!user.HasTitle(Character.UserTitles.Vip))
                            await user.AddTitleAsync(Character.UserTitles.Vip, user.VipExpiration);

                    EventManager.GetEvent<QuizShow>()?.Enter(user);
                    await client.SendAsync(this);
                    break;
                }

                case ActionType.CallPetJump:
                case ActionType.MapJump: // 133
                {
                    var newX = (ushort) Command;
                    var newY = (ushort) (Command >> 16);

                    // user and call pet handles jump
                    if (Identity == user.Identity)
                    {
                        if (!user.IsAlive)
                        {
                            await user.SendAsync(Language.StrDead, TalkChannel.System, Color.Red);
                            return;
                        }

                        if (user.GetDistance(newX, newY) >= 2 * Screen.VIEW_SIZE)
                        {
                            await user.SendAsync(Language.StrInvalidMsg, TalkChannel.System, Color.Red);
                            await RoleManager.KickOutAsync(user.Identity,
                                                           $"big jump [{user.MapX},{user.MapY}] -> [{newX},{newY}] = {user.GetDistance(newX, newY)}");
                            return;
                        }
                    }
                    else if (role?.IsCallPet() != true || role.Identity != user.GetCallPet()?.Identity)
                    {
                        return;
                    }

#if DEBUG_MOVEMENT
                        Console.WriteLine($"{user.Name} jump [{user.MapX},{user.MapY}] -> [{newX},{newY}]");
#endif

                    await role.ProcessOnMoveAsync();
                    bool result = await role.JumpPosAsync(newX, newY);
                    Character couple;
                    if (result 
                        && user.HasCoupleInteraction()
                        && user.HasCoupleInteractionStarted()
                        && (couple = user.GetCoupleInteractionTarget()) != null)
                    {
                        await couple.ProcessOnMoveAsync();
                        couple.MapX = user.MapX;
                        couple.MapY = user.MapY;
                        await couple.ProcessAfterMoveAsync();

                        await user.SendAsync(this);
                        await Kernel.BroadcastWorldMsgAsync(this);
                        Identity = couple.Identity;
                        await couple.SendAsync(this);
                        await Kernel.BroadcastWorldMsgAsync(this);

                        MsgSyncAction msg = new MsgSyncAction
                        {
                            Action = SyncAction.Jump,
                            X = user.MapX,
                            Y = user.MapY
                        };
                        msg.Targets.Add(user.Identity);
                        msg.Targets.Add(couple.Identity);
                        await user.SendAsync(msg);
                        await user.Screen.UpdateAsync(msg);
                        await couple.Screen.UpdateAsync();
                    }
                    else
                    {
                        if (role.IsPlayer())
                        {
                            await role.SendAsync(this);
                        }

                        if (role.Screen != null)
                        {
                            await role.Screen.UpdateAsync(this);
                        }
                        else
                        {
                            await role.BroadcastRoomMsgAsync(this, true);
                        }

                        await Kernel.BroadcastWorldMsgAsync(this);
                    }

                    break;
                }

                case ActionType.RelationshipsFriend: // 140
                {
                    if (user == null)
                        return;

                    Friend fetchFriend = user.GetFriend(Command);
                    if (fetchFriend == null)
                    {
                        await user.SendAsync(this);
                        return;
                    }

                    await fetchFriend.SendInfoAsync();
                    break;
                }

                case ActionType.CharacterDead: // 145
                {
                    if (user == null)
                        return;

                    if (user.IsAlive)
                        return;

                    await user.SetGhostAsync();
                    break;
                }

                case ActionType.CharacterAvatar:
                {
                    if (user == null)
                        return;

                    if (user.Gender == 1 && Command >= 200 || user.Gender == 2 && Command < 200)
                        return;

                    user.Avatar = (ushort) Command;
                    await user.BroadcastRoomMsgAsync(this, true);
                    await user.SaveAsync();
                    break;
                }

                case ActionType.QueryTradeBuddy: // 143
                {
                    if (user == null)
                        return;

                    TradePartner partner = user.GetTradePartner(Command);
                    if (partner == null)
                    {
                        await user.SendAsync(this);
                        return;
                    }

                    await partner.SendInfoAsync();
                    break;
                }

                case ActionType.ItemDetained:
                {
                    break;
                }

                case ActionType.Away:
                {
                    if (user == null)
                        return;

                    user.IsAway = Data != 0;

                    if (user.IsAway && user.Action != EntityAction.Sit)
                        await user.SetActionAsync(EntityAction.Sit, true);
                    else if (!user.IsAway && user.Action == EntityAction.Sit)
                        await user.SetActionAsync(EntityAction.Stand, true);

                    await user.BroadcastRoomMsgAsync(this, true);
                    break;
                }

                case ActionType.FriendObservation: // 310
                {
                    if (user == null)
                        return;

                    targetUser = RoleManager.GetUser(Command);
                    if (targetUser == null)
                        return;

                    await targetUser.SendWindowToAsync(user);
                    break;
                }

                default:
                {
                    if (client != null)
                    {
                        await client.SendAsync(this);
                        if (client.Character.IsPm())
                            await client.SendAsync(new MsgTalk(client.Identity, TalkChannel.Service,
                                                               $"Missing packet {Type}, Action {Action}, Length {Length}"));
                    }

                    await Log.WriteLogAsync(LogLevel.Warning,
                                            "Missing packet {0}, Action {1}, Length {2}\n{3}",
                                            Type, Action, Length, PacketDump.Hex(Encode()));
                    break;
                }
            }
        }
    }
}