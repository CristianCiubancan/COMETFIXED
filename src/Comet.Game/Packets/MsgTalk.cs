using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Packets.Ai;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Game.States.Syndicates;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1004</remarks>
    /// <summary>
    ///     Message defining a chat message from one player to the other, or from the system
    ///     to a player. Used for all chat systems in the game, including messages outside of
    ///     the game world state, such as during character creation or to tell the client to
    ///     continue logging in after connect.
    /// </summary>
    public sealed class MsgTalk : MsgTalk<Client>
    {
        public MsgTalk()
        {
        }

        public MsgTalk(uint characterID, TalkChannel channel, string text)
            : base(characterID, channel, text)
        {
        }

        public MsgTalk(uint characterID, TalkChannel channel, Color color, string text)
            : base(characterID, channel, color, text)
        {
        }

        public MsgTalk(uint characterID, TalkChannel channel, Color color, string recipient, string sender, string text)
            : base(characterID, channel, color, recipient, sender, text)
        {
        }

        public static MsgTalk LoginOk { get; } = new(0, TalkChannel.Login, "ANSWER_OK");
        public static MsgTalk LoginInvalid { get; } = new(0, TalkChannel.Login, "Invalid login");
        public static MsgTalk LoginNewRole { get; } = new(0, TalkChannel.Login, "NEW_ROLE");
        public static MsgTalk RegisterOk { get; } = new(0, TalkChannel.Register, "ANSWER_OK");
        public static MsgTalk RegisterInvalid { get; } = new(0, TalkChannel.Register, "Invalid character");
        public static MsgTalk RegisterNameTaken { get; } = new(0, TalkChannel.Register, "Character name taken");
        public static MsgTalk RegisterTryAgain { get; } = new(0, TalkChannel.Register, "Error, please try later");

        public override async Task ProcessAsync(Client client)
        {
            Character sender = client.Character;
            Character target = RoleManager.GetUser(RecipientName);

            if (sender.Name != SenderName)
            {
#if DEBUG
                if (sender.IsGm())
                    await sender.SendAsync("Invalid sender name????");
#endif
                return;
            }

            if (sender.IsGm() || target?.IsGm() == true)
                await Log.GmLogAsync("gm_talk", $"{sender.Name} says to {RecipientName}: {Message}");

            // if (Channel != TalkChannel.Whisper) // for privacy
            {
                await ServerDbContext.SaveAsync(new DbMessageLog
                {
                    SenderIdentity = sender.Identity,
                    SenderName = sender.Name,
                    TargetIdentity = target?.Identity ?? 0,
                    TargetName = target?.Name ?? RecipientName,
                    Channel = (ushort) Channel,
                    Message = Message,
                    Time = DateTime.Now
                });
            }

            if (await ProcessCommandAsync(Message, sender))
            {
                await Log.GmLogAsync("gm_cmd", $"{sender.Name}: {Message}");
                return;
            }

            switch (Channel)
            {
                case TalkChannel.Talk:
                {
                    if (!sender.IsAlive)
                        return;

                    await sender.BroadcastRoomMsgAsync(this, false);
                    break;
                }

                case TalkChannel.Whisper:
                {
                    if (target == null)
                    {
                        await sender.SendAsync(Language.StrTargetNotOnline, TalkChannel.Talk, Color.White);
                        return;
                    }

                    SenderMesh = sender.Mesh;
                    RecipientMesh = target.Mesh;

                    await target.SendAsync(this);
                    break;
                }

                case TalkChannel.Team:
                {
                    if (sender.Team != null)
                        await sender.Team.SendAsync(this, sender.Identity);
                    break;
                }

                case TalkChannel.Friend:
                {
                    await sender.SendToFriendsAsync(this);
                    break;
                }

                case TalkChannel.Guild:
                {
                    if (sender.SyndicateIdentity == 0)
                        return;

                    await sender.Syndicate.SendAsync(this, sender.Identity);
                    break;
                }

                case TalkChannel.Family:
                {
                    if (sender.FamilyIdentity == 0)
                        return;

                    await sender.Family.SendAsync(this, sender.Identity);
                    break;
                }

                case TalkChannel.Ghost:
                {
                    if (sender.IsAlive)
                        return;

                    await sender.BroadcastRoomMsgAsync(this, false);
                    break;
                }

                case TalkChannel.Announce:
                {
                    if (sender.SyndicateIdentity == 0 ||
                        sender.SyndicateRank != SyndicateMember.SyndicateRank.GuildLeader)
                        return;

                    sender.Syndicate.Announce = Message.Substring(0, Math.Min(127, Message.Length));
                    sender.Syndicate.AnnounceDate = DateTime.Now;
                    await sender.Syndicate.SaveAsync();
                    break;
                }

                case TalkChannel.Bbs:
                case TalkChannel.GuildBoard:
                case TalkChannel.FriendBoard:
                case TalkChannel.OthersBoard:
                case TalkChannel.TeamBoard:
                case TalkChannel.TradeBoard:
                {
                    MessageBoard.AddMessage(sender, Message, Channel);
                    break;
                }

                default:
                {
                    await Log.WriteLogAsync($"Unhandled MsgTalk: {Channel}");
                    break;
                }
                //case TalkChannel.World:
                //    {
                //        if (sender.CanUseWorldChat())
                //            return;

                //        await RoleManager.BroadcastMsgAsync(this);
                //        break;
                //    }
            }
        }

        private async Task<bool> ProcessCommandAsync(string fullCmd, Character user)
        {
            if (fullCmd.StartsWith("#") && user.Gender == 2 && fullCmd.Length > 7)
                // let's suppose that the user is with flower charm
                fullCmd = fullCmd[3..^3];

            if (fullCmd[0] != '/')
                return false;

            string[] splitCmd = fullCmd.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
            string cmd = splitCmd[0];
            var param = "";
            if (splitCmd.Length > 1)
                param = splitCmd[1];

            if (user.IsPm())
                switch (cmd.ToLower())
                {
                    case "/pro":
                    {
                        if (byte.TryParse(param, out byte proProf))
                            await user.SetAttributesAsync(ClientUpdateType.Class, proProf);

                        return true;
                    }

                    case "/life":
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Hitpoints, user.MaxLife);
                        return true;
                    }

                    case "/mana":
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Mana, user.MaxMana);
                        return true;
                    }

                    case "/restore":
                    {
                        if (!user.IsAlive)
                        {
                            await user.RebornAsync(false, true);
                        }
                        else
                        {
                            await user.SetAttributesAsync(ClientUpdateType.Hitpoints, user.MaxLife);
                            await user.SetAttributesAsync(ClientUpdateType.Mana, user.MaxMana);
                        }

                        return true;
                    }

                    case "/superman":
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Strength, 225);
                        await user.SetAttributesAsync(ClientUpdateType.Agility, 256);
                        await user.SetAttributesAsync(ClientUpdateType.Vitality, 225);
                        await user.SetAttributesAsync(ClientUpdateType.Spirit, 225);

                        return true;
                    }

                    case "/uplev":
                    {
                        if (byte.TryParse(param, out byte uplevValue))
                            await user.AwardLevelAsync(uplevValue);

                        return true;
                    }

                    case "/awardbattlexp":
                    {
                        if (!long.TryParse(param, out var exp))
                            return true;

                        await user.AwardBattleExpAsync(exp, true);
                        return true;
                    }

                    case "/awardexpball":
                    {
                        if (!int.TryParse(param, out var amount))
                            return true;

                        var exp = user.CalculateExpBall(Math.Max(1, amount) * Role.EXPBALL_AMOUNT);
                        await user.AwardExperienceAsync(exp);
                        return true;
                    }

                    case "/awarditem":
                    {
                        if (!uint.TryParse(param, out uint idAwardItem))
                            return true;

                        DbItemtype itemtype = ItemManager.GetItemtype(idAwardItem);
                        if (itemtype == null)
                        {
                            await user.SendAsync($"[AwardItem] Itemtype {idAwardItem} not found");
                            return true;
                        }

                        await user.UserPackage.AwardItemAsync(idAwardItem);
                        return true;
                    }

                    case "/awardmoney":
                    {
                        if (int.TryParse(param, out int moneyAmount))
                            await user.AwardMoneyAsync(moneyAmount);
                        return true;
                    }
                    case "/awardemoney":
                    {
                        if (int.TryParse(param, out int emoneyAmount))
                            await user.AwardConquerPointsAsync(emoneyAmount);
                        return true;
                    }

                    case "/awardmagic":
                    case "/awardskill":
                    {
                        byte skillLevel = 0;
                        string[] awardSkill = param.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (!ushort.TryParse(awardSkill[0], out ushort skillType))
                            return true;
                        if (awardSkill.Length > 1 && !byte.TryParse(awardSkill[1], out skillLevel))
                            return true;

                        Magic magic;
                        if (user.MagicData.CheckType(skillType))
                        {
                            magic = user.MagicData[skillType];
                            magic.Level = Math.Min(magic.MaxLevel, Math.Max((byte) 0, skillLevel));
                            await magic.SaveAsync();
                            await magic.SendAsync();
                        }
                        else
                        {
                            if (!await user.MagicData.CreateAsync(skillType, skillLevel))
                                await user.SendAsync("[Award Skill] Could not create skill!");
                        }

                        return true;
                    }

                    case "/awardwskill":
                    {
                        byte level = 1;

                        string[] awardwskill = param.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                        if (!ushort.TryParse(awardwskill[0], out ushort type))
                            return true;
                        if (awardwskill.Length > 1 && !byte.TryParse(awardwskill[1], out level))
                            return true;

                        if (user.WeaponSkill[type] == null)
                        {
                            await user.WeaponSkill.CreateAsync(type, level);
                        }
                        else
                        {
                            user.WeaponSkill[type].Level = level;
                            await user.WeaponSkill.SaveAsync(user.WeaponSkill[type]);
                            await user.WeaponSkill.SendAsync(user.WeaponSkill[type]);
                        }

                        return true;
                    }
                    case "/status":
                    {
                        if (int.TryParse(param, out int flag)) await user.AttachStatusAsync(user, flag, 0, 10, 0, 0);
                        return true;
                    }

                    case "/creategen":
                    {
                        await user.SendAsync(
                            "Attention, use this command only on localhost tests or the generator thread may crash.");
                        // mobid mapid mapx mapy boundcx boundcy maxnpc rest maxpergen
                        string[] szComs = param.Split(' ');
                        if (szComs.Length < 9)
                        {
                            await user.SendAsync(
                                "/creategen mobid mapid mapx mapy boundcx boundcy maxnpc rest maxpergen");
                            return true;
                        }

                        ushort idMob = ushort.Parse(szComs[0]);
                        uint idMap = uint.Parse(szComs[1]);
                        ushort mapX = ushort.Parse(szComs[2]);
                        ushort mapY = ushort.Parse(szComs[3]);
                        ushort boundcx = ushort.Parse(szComs[4]);
                        ushort boundcy = ushort.Parse(szComs[5]);
                        ushort maxNpc = ushort.Parse(szComs[6]);
                        ushort restSecs = ushort.Parse(szComs[7]);
                        ushort maxPerGen = ushort.Parse(szComs[8]);

                        if (idMap == 0) idMap = user.MapIdentity;

                        if (mapX == 0 || mapY == 0)
                        {
                            mapX = user.MapX;
                            mapY = user.MapY;
                        }

                        var newGen = new DbGenerator
                        {
                            Mapid = idMap,
                            Npctype = idMob,
                            BoundX = mapX,
                            BoundY = mapY,
                            BoundCx = boundcx,
                            BoundCy = boundcy,
                            MaxNpc = maxNpc,
                            RestSecs = restSecs,
                            MaxPerGen = maxPerGen,
                            BornX = 0,
                            BornY = 0,
                            TimerBegin = 0,
                            TimerEnd = 0
                        };

                        if (!await ServerDbContext.SaveAsync(newGen))
                        {
                            await user.SendAsync("Could not save generator.");
                            return true;
                        }

                        await Kernel.BroadcastWorldMsgAsync(new MsgAiGeneratorManage(newGen));
                        //Generator pGen = new Generator(newGen);
                        //await pGen.GenerateAsync();
                        ////await Kernel.WorldThread.AddGeneratorAsync(pGen);
                        //await GeneratorManager.AddGeneratorAsync(pGen);
                        return true;
                    }

                    case "/action":
                    {
                        if (uint.TryParse(param, out uint idExecuteAction))
                            await GameAction.ExecuteActionAsync(idExecuteAction, user, null, null, string.Empty);
                        return true;
                    }

                    case "/reloadactionall":
                    {
                        await EventManager.ReloadActionTaskAllAsync();
                        return true;
                    }

                    case "/xp":
                    {
                        await user.AddXpAsync(100);
                        await user.BurstXpAsync();
                        return true;
                    }

                    case "/sp":
                    {
                        await user.SetAttributesAsync(ClientUpdateType.Stamina, user.MaxEnergy);
                        return true;
                    }

                    case "/querynpcs":
                    {
                        foreach (BaseNpc npc in user
                                                .Map.Query9BlocksByPos(user.MapX, user.MapY).Where(x => x is BaseNpc)
                                                .Cast<BaseNpc>())
                            await user.SendAsync($"NPC[{npc.Identity}]:{npc.Name}({npc.MapX},{npc.MapY})",
                                                 TalkChannel.Talk);
                        return true;
                    }

                    case "/movenpc":
                    {
                        string[] moveNpcParams =
                            param.Trim().Split(new[] {" "}, 4, StringSplitOptions.RemoveEmptyEntries);

                        if (moveNpcParams.Length < 4)
                        {
                            await user.SendAsync("Move NPC cmd must have: npcid mapid targetx targety");
                            return true;
                        }

                        if (!uint.TryParse(moveNpcParams[0], out uint idNpc)
                            || !uint.TryParse(moveNpcParams[1], out uint idMap)
                            || !ushort.TryParse(moveNpcParams[2], out ushort mapX)
                            || !ushort.TryParse(moveNpcParams[3], out ushort mapY))
                            return true;

                        var npc = RoleManager.GetRole<BaseNpc>(idNpc);
                        if (npc == null)
                        {
                            await user.SendAsync($"Object {idNpc} is not of type npc");
                            return true;
                        }

                        GameMap map = MapManager.GetMap(idMap);
                        if (map == null)
                            return true;

                        if (!map.IsValidPoint(mapX, mapY))
                            return true;

                        await npc.ChangePosAsync(idMap, mapX, mapY);
                        return true;
                    }


                    case "/toggleaction":
                    {
                        user.ShowAction = !user.ShowAction;
                        return true;
                    }

                    case "/vip":
                    {
                        if (byte.TryParse(param, out byte vip))
                        {
                            if (!user.HasVip)
                                user.VipExpiration = DateTime.Now.AddDays(1);
                            await user.SetAttributesAsync(ClientUpdateType.VipLevel, vip);
                        }

                        return true;
                    }

                    case "/testpb": // test progress bar
                    {
                        var msg = new MsgAction();
                        msg.Action = MsgAction<Client>.ActionType.ProgressBar;
                        msg.Identity = user.Identity;
                        msg.Command = 8;
                        msg.MapColor = 5;
                        msg.Strings.Add("Teste");
                        await user.SendAsync(msg);
                        return true;
                    }

                    case "/msgmentorplayer":
                    {
                        /*
                             * >> Only offset 12 filled with ID of user (Received animation)
                             * 
                             * >> Offset 8 sender and 12 target animation in both characters
                             */
                        string[] @params = param.Trim().Split(' ');
                        using var writer = new PacketWriter();
                        writer.Write((ushort) PacketType.MsgMentorPlayer);
                        writer.Write(0);       // 4
                        writer.Write(1000004); // Sender 8 [1000001]
                        writer.Write(1000001); // Target 12 [1000004]
                        writer.Write(0);       // 16
                        writer.Write(0);       // 20
                        writer.Write(0);       // 24
                        await user.Client.SendAsync(writer.ToArray());
                        return true;
                    }

                    case "/msgrank":
                    {
                        await using var writer = new PacketWriter();
                        writer.Write((ushort) PacketType.MsgRank);
                        writer.Write(2);         // 4
                        writer.Write(0x1c9c4ae); // 8 
                        writer.Write(0);         // 12
                        writer.Write(1);         // 16
                        writer.Write(0);         // 20
                        writer.Write(1);         // 24
                        writer.Write(0);         // 28
                        writer.Write(0);         // 32
                        writer.Write(0);         // 36
                        writer.Write(0);         // 40
                        writer.Write(1000001);   // 44
                        writer.Write(0);         // 48
                        writer.Write(0);         // 52
                        writer.Write(0);         // 56
                        writer.Write(0);         // 60
                        writer.Write(0);         // 64
                        writer.Write(0);         // 68
                        writer.Write(0);         // 72
                        await user.Client.SendAsync(writer.ToArray());

                        await user.SendAsync(new MsgRank
                        {
                            Mode = (MsgRank<Client>.RequestType) 5
                        });
                        return true;
                    }

                    case "/msgflower":
                    {
                        var msg = new MsgFlower();
                        msg.SenderName = user.Name;
                        msg.ReceiverName = "Teste";
                        msg.SendAmount = 1;
                        await user.SendAsync(msg);

                        //Console.WriteLine("MsgFlower: " + PacketDump.Hex(msg.Encode()));
                        return true;
                    }

                    case "/msgplayer":
                    {
                        var startOffset = 0;
                        var endOffset = 1;
                        long value = 0;
                        byte size = 0;

                        bool overrideOffset = false;
                        string[] splitParam = param.Split(' ');
                        if (splitParam.Length >= 3)
                        {
                            byte.TryParse(splitParam[0], out size);
                            long.TryParse(splitParam[1], out value);
                            int.TryParse(splitParam[2], out startOffset);
                            endOffset = startOffset + 1;
                            overrideOffset = true;
                        }
                        if (splitParam.Length >= 4)
                        {
                            int.TryParse(splitParam[3], out endOffset);
                            overrideOffset = true;
                        }

                        int x = 0,
                            y = 0;

                        int offset = startOffset;
                        do
                        {
                            var mapX = (ushort) (user.MapX + x++);
                            if (x >= 10)
                            {
                                x = 0;
                                y += 1;
                            }

                            var mapY = (ushort) (user.MapY + y);

                            await using var writer = new PacketWriter();
                            writer.Write((ushort) PacketType.MsgPlayer);
                            writer.Write(user.Mesh);                            // 4
                            writer.Write(_testUserId++);                        // 8
                            writer.Write((int) user.SyndicateIdentity);         // 12
                            writer.Write(900);                                  // 16
                            writer.Write((ushort) 0);                           // 20
                            writer.Write(0UL);                                  // 22
                            writer.Write(user.Headgear?.Type ?? 0);             // 30
                            writer.Write(user.Garment?.Type ?? 0);              // 34
                            writer.Write(user.Armor?.Type ?? 0);                // 38
                            writer.Write(user.RightHand?.Type ?? 0);            // 42
                            writer.Write(user.LeftHand?.Type ?? 0);             // 46
                            writer.Write(user.Mount?.Type ?? 0);                // 50
                            writer.Write(0);                                    // 54
                            writer.Write((ushort) 0);                           // 58
                            writer.Write((ushort) 0);                           // 60
                            writer.Write(user.Hairstyle);                       // 62
                            writer.Write(mapX);                                 // 64
                            writer.Write(mapY);                                 // 66
                            writer.Write((byte) user.Direction);                // 68
                            writer.Write((byte) user.Action);                   // 69
                            writer.Write(0);                                    // 70
                            writer.Write(user.Metempsychosis);                  // 74
                            writer.Write((ushort) 140);                          // 75
                            writer.Write(false);                                // 77
                            writer.Write(user.IsAway);                          // 78
                            writer.Write(0);                                    // 79
                            writer.Write(0);                                    // 83
                            writer.Write(0);                                    // 87
                            writer.Write(0);                                    // 91
                            writer.Write((int) user.NobilityRank);              // 95
                            writer.Write((ushort) (user.Armor?.Color ?? 0));    // 99
                            writer.Write((ushort) (user.LeftHand?.Color ?? 0)); // 101
                            writer.Write((ushort) (user.Headgear?.Color ?? 0)); // 103
                            writer.Write(user.QuizPoints);                      // 105
                            writer.Write(user.Mount?.Plus ?? 0);                // 109
                            writer.Write(0);                                    // 110
                            writer.Write((int) (user.Mount?.Color ?? 0));       // 114
                            writer.Write((byte) 0);                             // 118
                            writer.Write((ushort) 0);                           // 119
                            writer.Write((byte) 0);                             // 121
                            writer.Write((byte) 0);                             // 122
                            writer.Write((byte) 0);                             // 123
                            writer.Write((byte) 0);                             // 124
                            writer.Write((byte) 0);                             // 125
                            writer.Write((byte) 0);                             // 126
                            writer.Write((byte) 0);                             // 127
                            writer.Write((byte) 0);                             // 128
                            writer.Write((byte) 0);                             // 129
                            writer.Write((byte) 0);                             // 130
                            writer.Write(user.FamilyIdentity);                  // 131
                            writer.Write(10);                                   // 135
                            writer.Write(user.FamilyBattlePower);               // 139
                            writer.Write((int) user.UserTitle);                 // 143
                            writer.Write(0);                                    // 147
                            writer.Write(0);                                    // 151
                            writer.Write(new List<string>                       // 155
                            {
                                $"{_testUserId - 1}_123",
                                user.FamilyName
                            });

                            if (overrideOffset)
                            {
                                writer.BaseStream.Position = offset;
                                switch (size)
                                {
                                    case 1:
                                        writer.Write((byte) value);
                                        break;
                                    case 2:
                                        writer.Write((ushort) value);
                                        break;
                                    case 4:
                                        writer.Write((int) value);
                                        break;
                                    case 8:
                                        writer.Write(value);
                                        break;
                                }
                            }

                            await user.Client.SendAsync(writer.ToArray());
                        }
                        while (++offset < endOffset);

                        return true;
                    }
                }

            if (user.IsGm())
                switch (cmd.ToLower())
                {
                    case "/bring":
                    {
                        Character bringTarget;
                        if (uint.TryParse(param, out uint idFindTarget))
                            bringTarget = RoleManager.GetUser(idFindTarget);
                        else
                            bringTarget = RoleManager.GetUser(param);

                        if (bringTarget == null)
                        {
                            await user.SendAsync("Target not found");
                            return true;
                        }

                        await bringTarget.FlyMapAsync(user.MapIdentity, user.MapX, user.MapY);
                        return true;
                    }
                    case "/cmd":
                    {
                        string[] cmdParams = param.Split(new[] {' '}, 2, StringSplitOptions.RemoveEmptyEntries);
                        string subCmd = cmdParams[0];

                        if (cmd.Length > 1)
                        {
                            string subParam = cmdParams[1];

                            switch (subCmd.ToLower())
                            {
                                case "broadcast":
                                    await RoleManager.BroadcastMsgAsync(subParam, TalkChannel.Center,
                                                                        Color.White);
                                    break;

                                case "gmmsg":
                                    await RoleManager.BroadcastMsgAsync($"{user.Name} says: {subParam}",
                                                                        TalkChannel.Center, Color.White);
                                    break;

                                case "player":
                                    if (subParam.Equals("all", StringComparison.InvariantCultureIgnoreCase))
                                        await user.SendAsync(
                                            $"Players Online: {RoleManager.OnlinePlayers}, Distinct: {RoleManager.OnlineUniquePlayers} (max: {RoleManager.MaxOnlinePlayers})",
                                            TalkChannel.TopLeft, Color.White);
                                    else if (subParam.Equals("map", StringComparison.InvariantCultureIgnoreCase))
                                        await user.SendAsync(
                                            $"Map Online Players: {user.Map.PlayerCount} ({user.Map.Name})",
                                            TalkChannel.TopLeft, Color.White);

                                    break;
                            }

                            return true;
                        }

                        return true;
                    }

                    case "/chgmap":
                    {
                        string[] chgMapParams = param.Split(new[] {' '}, 3, StringSplitOptions.RemoveEmptyEntries);
                        if (chgMapParams.Length < 3)
                            return true;

                        if (uint.TryParse(chgMapParams[0], out uint chgMapId)
                            && ushort.TryParse(chgMapParams[1], out ushort chgMapX)
                            && ushort.TryParse(chgMapParams[2], out ushort chgMapY))
                        {
                            var error = false;
                            List<Role> roleSet = user.Map.Query9Blocks(chgMapX, chgMapY);
                            foreach (Role role in roleSet)
                                if (role is BaseNpc npc
                                    && role.MapX == chgMapX && role.MapY == chgMapY)
                                {
                                    error = true;
                                    break;
                                }

                            if (!error)
                                await user.FlyMapAsync(chgMapId, chgMapX, chgMapY);
                        }

                        return true;
                    }

                    case "/openui":
                    {
                        if (uint.TryParse(param, out uint ui))
                            await user.SendAsync(new MsgAction
                            {
                                Action = MsgAction<Client>.ActionType.ClientCommand,
                                Identity = user.Identity,
                                Command = ui,
                                ArgumentX = user.MapX,
                                ArgumentY = user.MapY
                            });
                        return true;
                    }

                    case "/openwindow":
                    {
                        if (uint.TryParse(param, out uint window))
                            await user.SendAsync(new MsgAction
                            {
                                Action = MsgAction<Client>.ActionType.ClientDialog,
                                Identity = user.Identity,
                                Command = window,
                                ArgumentX = user.MapX,
                                ArgumentY = user.MapY
                            });
                        return true;
                    }
                    case "/kickout":
                    {
                        Character findTarget;
                        if (uint.TryParse(param, out uint idFindTarget))
                            findTarget = RoleManager.GetUser(idFindTarget);
                        else
                            findTarget = RoleManager.GetUser(param);

                        if (findTarget == null)
                        {
                            await user.SendAsync("Target not found");
                            return true;
                        }

                        try
                        {
                            findTarget.Client.Disconnect();
                        }
                        catch (Exception ex)
                        {
                            await Log.WriteLogAsync("kickout_ex", LogLevel.Exception, ex.ToString());
                            RoleManager.ForceLogoutUser(findTarget.Identity);
                        }

                        return true;
                    }

                    case "/find":
                    {
                        Character findTarget;
                        if (uint.TryParse(param, out uint idFindTarget))
                            findTarget = RoleManager.GetUser(idFindTarget);
                        else
                            findTarget = RoleManager.GetUser(param);

                        if (findTarget == null)
                        {
                            await user.SendAsync("Target not found");
                            return true;
                        }

                        await user.FlyMapAsync(findTarget.MapIdentity, findTarget.MapX, findTarget.MapY);
                        return true;
                    }

                    case "/bot":
                    {
                        string[] myParams = param.Split(new[] {" "}, 2, StringSplitOptions.RemoveEmptyEntries);

                        if (myParams.Length < 2)
                        {
                            await user.SendAsync("/bot [target_name] [reason]", TalkChannel.Talk);
                            return true;
                        }

                        Character target = RoleManager.GetUser(myParams[0]);
                        if (target != null)
                        {
                            await Log.GmLogAsync(
                                "botjail",
                                $"{user.Identity} {user.Name} botjailed {target.Identity} {target.Name} by: {myParams[1]}");
                            await target.SendAsync(Language.StrBotjail);
                            await target.FlyMapAsync(6002, 28, 74);
                            await target.SaveAsync();
                        }

                        return true;
                    }

                    case "/macro":
                    {
                        string[] myParams = param.Split(new[] {" "}, 2, StringSplitOptions.RemoveEmptyEntries);

                        if (myParams.Length < 2)
                        {
                            await user.SendAsync("/macro [target_name] [reason]", TalkChannel.Talk);
                            return true;
                        }

                        Character target = RoleManager.GetUser(myParams[0]);
                        if (target != null)
                        {
                            await Log.GmLogAsync("macrojail",
                                                 $"{user.Identity} {user.Name} macrojailed {target.Identity} {target.Name} by: {myParams[1]}");
                            await target.SendAsync(Language.StrMacrojail);
                            await target.FlyMapAsync(6010, 28, 74);
                            await target.SaveAsync();
                        }

                        return true;
                    }

                    case "/cancelevent":
                    {
                        var type = (GameEvent.EventType) int.Parse(param);
                        EventManager.RemoveEvent(type);
                        return true;
                    }

                    case "/player":
                    {
                        if (param.Equals("all"))
                        {
                            await user.SendAsync(
                                $"Players Online: {RoleManager.OnlinePlayers}, Unique Online Players: {RoleManager.OnlineUniquePlayers}, Max Online Players: {RoleManager.MaxOnlinePlayers}");
                        }
                        else if (param.Equals("map"))
                        {
                            await user.SendAsync($"Players Online at [{user.Map.Name}]: {user.Map.PlayerCount}");
                        }
                        else if (param.Equals("screen"))
                        {
                            await user.SendAsync(
                                $"Players around: {user.Screen.Roles.Values.Count(x => x.IsPlayer())}");
                        }
                        else if (param.Equals("partition"))
                        {
                            int count = RoleManager.QueryUserSet().Count(x => x.Map.Partition == user.Map.Partition);
                            await user.SendAsync($"Players Online on partition [{user.Map.Partition}]: {count}");
                        }

                        return true;
                    }

                    case "/fly":
                    {
                        string[] chgMapParams = param.Split(new[] {' '}, 3, StringSplitOptions.RemoveEmptyEntries);
                        if (chgMapParams.Length < 2)
                            return true;

                        int x = int.Parse(chgMapParams[0]);
                        int y = int.Parse(chgMapParams[1]);

                        if (!user.Map.IsStandEnable(x, y))
                        {
                            await user.SendAsync(Language.StrInvalidCoordinate);
                            return true;
                        }

                        var error = false;
                        List<Role> roleSet = user.Map.Query9Blocks(x, y);
                        foreach (Role role in roleSet)
                            if (role is BaseNpc npc
                                && role.MapX == x && role.MapY == y)
                            {
                                error = true;
                                break;
                            }

                        if (!error)
                            await user.FlyMapAsync(0, x, y);
                        else
                            await user.SendAsync(Language.StrInvalidCoordinate);
                        return true;
                    }
                }

            switch (cmd.ToLower())
            {
                case "/dc":
                case "/discnonect":
                {
                    user.Client.Disconnect();
                    return true;
                }

                case "/battleattr":
                {
                    Role target;
                    if (!string.IsNullOrEmpty(param) && uint.TryParse(param, out uint idBpTarget))
                        target = RoleManager.GetRole(idBpTarget) ?? user;
                    else if (!string.IsNullOrEmpty(param))
                        target = RoleManager.GetUser(param);
                    else
                        target = user;

                    if (target.Identity == user.Identity)
                        await user.SendAsync(
                            $"Battle Attributes for yourself: {user.Name} [Potency: {user.BattlePower}]",
                            TalkChannel.Talk, Color.White);
                    else
                        await user.SendAsync(
                            $"Battle Attributes for target: {target.Name} [Potency: {target.BattlePower}]",
                            TalkChannel.Talk, Color.White);

                    await user.SendAsync($"Life: {target.Life}-{target.MaxLife}, Mana: {target.Mana}-{target.MaxMana}",
                                         TalkChannel.Talk, Color.White);
                    await user.SendAsync(
                        $"Attack: {target.MinAttack}-{target.MaxAttack}, Magic Attack: {target.MagicAttack}",
                        TalkChannel.Talk, Color.White);
                    await user.SendAsync(
                        $"Defense: {target.Defense}, Defense2: {target.Defense2}, MagicDefense: {target.MagicDefense}, MagicDefenseBonus: {target.MagicDefenseBonus}%",
                        TalkChannel.Talk, Color.White);
                    await user.SendAsync(
                        $"Accuracy: {target.Accuracy}, Dodge: {target.Dodge}, Attack Speed: {target.AttackSpeed}",
                        TalkChannel.Talk, Color.White);
                    if (target is Character tgtUsr)
                        await user.SendAsync(
                            $"DG: {tgtUsr.DragonGemBonus}%, PG: {tgtUsr.PhoenixGemBonus}%, Blessing: {tgtUsr.Blessing}%, TG: {tgtUsr.TortoiseGemBonus}%",
                            TalkChannel.Talk, Color.White);
                    return true;
                }

                case "/onlinetime":
                {
                    await user.SendAsync(
                        $"You have been online on the current session for {user?.SessionOnlineTime.TotalDays:0} days, {user?.SessionOnlineTime.Hours:00} hours, {user?.SessionOnlineTime.Minutes} minutes and {user?.SessionOnlineTime.Seconds} seconds.",
                        TalkChannel.Talk);
                    await user.SendAsync(
                        $"You have been online in the game for {user?.OnlineTime.TotalDays:0} days, {user?.OnlineTime.Hours:00} hours, {user?.OnlineTime.Minutes} minutes and {user?.OnlineTime.Seconds} seconds",
                        TalkChannel.Talk);
                    return true;
                }

                case "/clearinventory":
                {
                    await user.UserPackage.ClearInventoryAsync();
                    await user.SendAsync(
                        "Your inventory has been cleaned! The usage of this command is of your own responsability.");
                    return true;
                }

                case "/mineproffit":
                {
                    var oreList = new List<Item>();
                    user.UserPackage.MultiGetItem(Item.IRON_ORE, Item.EUXINITE_ORE, int.MaxValue, ref oreList);
                    user.UserPackage.MultiGetItem(Item.SILVER_ORE, Item.GOLD_ORE + 9, int.MaxValue, ref oreList);

                    var amount = 0;
                    for (int i = oreList.Count - 1; i >= 0; i--)
                    {
                        Item item = oreList[i];
                        int price = item.GetSellPrice();
                        if (!await user.UserPackage.SpendItemAsync(item))
                            continue;
                        await user.AwardMoneyAsync(price);
                        amount += price;
                    }

                    await user.SendAsync($"You awarded {amount} silvers selling your ores.");
                    return true;
                }
            }

            return false;
        }

        private static int _testUserId = 100000000;
    }
}