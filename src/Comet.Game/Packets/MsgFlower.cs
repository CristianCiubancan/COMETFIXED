using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.Database;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgFlower : MsgFlower<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            switch (Mode)
            {
                case RequestMode.SendFlower:
                {
                    uint idTarget = Identity;

                    Character target = RoleManager.GetUser(idTarget);

                    if (!user.IsAlive)
                    {
                        await user.SendAsync(Language.StrFlowerSenderNotAlive);
                        return;
                    }

                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTargetNotOnline);
                        return;
                    }

                    if (user.Gender != 1)
                    {
                        await user.SendAsync(Language.StrFlowerSenderNotMale);
                        return;
                    }

                    if (target.Gender != 2)
                    {
                        await user.SendAsync(Language.StrFlowerReceiverNotFemale);
                        return;
                    }

                    if (user.Level < 50)
                    {
                        await user.SendAsync(Language.StrFlowerLevelTooLow);
                        return;
                    }

                    ushort amount = 0;
                    string flowerName = Language.StrFlowerNameRed;
                    var type = FlowerType.RedRose;
                    var effect = FlowerEffect.RedRose;
                    if (ItemIdentity == 0) // daily flower
                    {
                        if (user.SendFlowerTime != null
                            && int.Parse(user.SendFlowerTime.Value.ToString("yyyyMMdd")) >=
                            int.Parse(DateTime.Now.ToString("yyyyMMdd")))
                        {
                            await user.SendAsync(Language.StrFlowerHaveSentToday);
                            return;
                        }

                        switch (user.BaseVipLevel)
                        {
                            case 0:
                                amount = 1;
                                break;
                            case 1:
                                amount = 2;
                                break;
                            case 2:
                                amount = 5;
                                break;
                            case 3:
                                amount = 7;
                                break;
                            case 4:
                                amount = 9;
                                break;
                            case 5:
                                amount = 12;
                                break;
                            default:
                                amount = 30;
                                break;
                        }

                        user.SendFlowerTime = DateTime.Now;
                        await user.SaveAsync();
                    }
                    else
                    {
                        Item flower = user.UserPackage[ItemIdentity];
                        if (flower == null)
                            return;

                        switch (flower.GetItemSubType())
                        {
                            case 751:
                                type = FlowerType.RedRose;
                                effect = FlowerEffect.RedRose;
                                flowerName = Language.StrFlowerNameRed;
                                break;
                            case 752:
                                type = FlowerType.WhiteRose;
                                effect = FlowerEffect.WhiteRose;
                                flowerName = Language.StrFlowerNameWhite;
                                break;
                            case 753:
                                type = FlowerType.Orchid;
                                effect = FlowerEffect.Orchid;
                                flowerName = Language.StrFlowerNameLily;
                                break;
                            case 754:
                                type = FlowerType.Tulip;
                                effect = FlowerEffect.Tulip;
                                flowerName = Language.StrFlowerNameTulip;
                                break;
                        }

                        amount = flower.Durability;
                        await user.UserPackage.SpendItemAsync(flower);
                    }

                    FlowerManager.FlowerRankObject flowersToday = await FlowerManager.QueryFlowersAsync(target);
                    switch (type)
                    {
                        case FlowerType.RedRose:
                            target.FlowerRed += amount;
                            flowersToday.RedRose += amount;
                            flowersToday.RedRoseToday += amount;
                            break;
                        case FlowerType.WhiteRose:
                            target.FlowerWhite += amount;
                            flowersToday.WhiteRose += amount;
                            flowersToday.WhiteRoseToday += amount;
                            break;
                        case FlowerType.Orchid:
                            target.FlowerOrchid += amount;
                            flowersToday.Orchids += amount;
                            flowersToday.OrchidsToday += amount;
                            break;
                        case FlowerType.Tulip:
                            target.FlowerTulip += amount;
                            flowersToday.Tulips += amount;
                            flowersToday.TulipsToday += amount;
                            break;
                    }

                    await user.SendAsync(Language.StrFlowerSendSuccess);
                    if (ItemIdentity != 0 && amount >= 99)
                        await RoleManager.BroadcastMsgAsync(
                            string.Format(Language.StrFlowerGmPromptAll, user.Name, amount, flowerName, target.Name),
                            TalkChannel.Center);

                    await target.SendAsync(string.Format(Language.StrFlowerReceiverPrompt, user.Name));
                    await user.BroadcastRoomMsgAsync(new MsgFlower
                    {
                        Identity = Identity,
                        ItemIdentity = ItemIdentity,
                        SenderName = user.Name,
                        ReceiverName = target.Name,
                        SendAmount = amount,
                        SendFlowerType = type,
                        SendFlowerEffect = effect
                    }, true);

                    var msg = new MsgFlower();
                    msg.SenderName = user.Name;
                    msg.ReceiverName = target.Name;
                    msg.SendAmount = amount;
                    await user.SendAsync(msg);

                    await ServerDbContext.SaveAsync(flowersToday.GetDatabaseObject());
                    break;
                }
                default:
                {
                    await Log.WriteLogAsync(LogLevel.Error, $"Unhandled MsgFlower:{Mode}");
                    await Log.WriteLogAsync(LogLevel.Debug, PacketDump.Hex(Encode()));
                    return;
                }
            }
        }
    }
}