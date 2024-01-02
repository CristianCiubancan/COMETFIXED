using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgDataArray : MsgDataArray<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (Items.Count < 2)
                return;

            Item target = user.UserPackage[Items[0]];

            if (target == null)
                return;

            int oldAddition = target.Plus;
            switch (Action)
            {
                case DataArrayMode.Composition:
                {
                    if (target.Plus >= 12)
                    {
                        await user.SendAsync(Language.StrComposeItemMaxComposition);
                        return;
                    }

                    for (var i = 1; i < Items.Count; i++)
                    {
                        Item source = user.UserPackage[Items[i]];
                        if (source == null)
                            continue;

                        if (source.Type is < Item.TYPE_STONE1 or > Item.TYPE_STONE8)
                        {
                            if (source.IsWeaponOneHand())
                            {
                                if (!target.IsWeaponOneHand() && !target.IsWeaponProBased())
                                    continue;
                            }
                            else if (source.IsWeaponTwoHand())
                            {
                                if (source.IsBow() && !target.IsBow())
                                    continue;
                                if (!target.IsWeaponTwoHand())
                                    continue;
                            }

                            if (target.GetItemSort() != source.GetItemSort())
                                continue;

                            if (source.Plus == 0 || source.Plus > 8)
                                continue;
                        }

                        target.CompositionProgress += PlusAddLevelExp(source.Plus, false);
                        while (target.CompositionProgress >= GetAddLevelExp(target.Plus, false) && target.Plus < 12)
                            if (target.Plus < 12)
                            {
                                target.CompositionProgress -= GetAddLevelExp(target.Plus, false);
                                target.ChangeAddition();
                            }
                            else
                            {
                                target.CompositionProgress = 0;
                                break;
                            }

                        await user.UserPackage.SpendItemAsync(source);
                    }

                    break;
                }

                case DataArrayMode.CompositionSteedOriginal:
                case DataArrayMode.CompositionSteedNew:
                {
                    if (!target.IsMount())
                        return;

                    for (var i = 1; i < Items.Count; i++)
                    {
                        Item source = user.UserPackage[Items[i]];
                        if (source == null)
                            continue;

                        target.CompositionProgress += PlusAddLevelExp(source.Plus, true);
                        while (target.CompositionProgress >= GetAddLevelExp(target.Plus, false) && target.Plus < 12)
                            if (target.Plus < 12)
                            {
                                target.CompositionProgress -= GetAddLevelExp(target.Plus, false);
                                target.ChangeAddition();
                            }

                        if (Action == DataArrayMode.CompositionSteedNew)
                        {
                            var color1 = (int) target.SocketProgress;
                            var color2 = (int) source.SocketProgress;
                            int B1 = color1 & 0xFF;
                            int B2 = color2 & 0xFF;
                            int G1 = (color1 >> 8) & 0xFF;
                            int G2 = (color2 >> 8) & 0xFF;
                            int R1 = (color1 >> 16) & 0xFF;
                            int R2 = (color2 >> 16) & 0xFF;
                            int newB = (int) Math.Floor(0.9 * B1) + (int) Math.Floor(0.1 * B2);
                            int newG = (int) Math.Floor(0.9 * G1) + (int) Math.Floor(0.1 * G2);
                            int newR = (int) Math.Floor(0.9 * R1) + (int) Math.Floor(0.1 * R2);
                            target.ReduceDamage = (byte) newR;
                            target.Enchantment = (byte) newB;
                            target.AntiMonster = (byte) newG;
                            target.SocketProgress = (uint) (newG | (newB << 8) | (newR << 16));
                        }

                        await user.UserPackage.SpendItemAsync(source);
                    }

                    break;
                }

                default:
                    await Log.WriteLogAsync(LogLevel.Error, $"Invalid MsgDataArray Action: {Action}." +
                                                            $"{user.Identity},{user.Name},{user.Level},{user.MapIdentity}[{user.Map.Name}],{user.MapX},{user.MapY}");
                    return;
            }

            if (oldAddition < target.Plus && target.Plus >= 6)
            {
                if (user.Gender == 1)
                    await RoleManager.BroadcastMsgAsync(string.Format(Language.StrComposeOverpowerMale, user.Name,
                                                                      target.Itemtype.Name, target.Plus),
                                                        TalkChannel.TopLeft);
                else
                    await RoleManager.BroadcastMsgAsync(string.Format(Language.StrComposeOverpowerFemale, user.Name,
                                                                      target.Itemtype.Name, target.Plus),
                                                        TalkChannel.TopLeft);
            }

            await target.SaveAsync();
            await user.SendAsync(new MsgItemInfo(target, MsgItemInfo<Client>.ItemMode.Update));
        }

        private static ushort PlusAddLevelExp(uint plus, bool steed)
        {
            switch (plus)
            {
                case 0:
                    if (steed) return 1;
                    return 0;
                case 1:  return 10;
                case 2:  return 40;
                case 3:  return 120;
                case 4:  return 360;
                case 5:  return 1080;
                case 6:  return 3240;
                case 7:  return 9720;
                case 8:  return 29160;
                default: return 0;
            }
        }

        public static ushort GetAddLevelExp(uint plus, bool steed)
        {
            switch (plus)
            {
                case 0: return 20;
                case 1: return 20;
                case 2:
                    if (steed) return 90;
                    return 80;
                case 3:  return 240;
                case 4:  return 720;
                case 5:  return 2160;
                case 6:  return 6480;
                case 7:  return 19440;
                case 8:  return 58320;
                case 9:  return 2700;
                case 10: return 5500;
                case 11: return 9000;
                default: return 0;
            }
        }
    }
}