using System;
using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgGemEmbed : MsgGemEmbed<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            if (Identity != user.Identity)
            {
                await Log.GmLogAsync("cheat", $"MsgGemEmbed invalid user identity {Identity} != {user.Identity}");
                return;
            }

            Item main = user.UserPackage[MainIdentity];
            if (main == null)
                return;

            switch (Action)
            {
                case EmbedAction.Embed:
                    Item minor = user.UserPackage[MinorIdentity];
                    if (minor == null || minor.GetItemSubType() != 700)
                    {
                        await user.SendAsync(Language.StrNoGemEmbed);
                        return;
                    }

                    var gem = (Item.SocketGem) (minor.Type % 1000);
                    if (!Enum.IsDefined(typeof(Item.SocketGem), (byte) gem))
                    {
                        await user.SendAsync(Language.StrNoGemEmbed);
                        return;
                    }

                    if (main.GetItemSubType() == 201)
                        switch (gem)
                        {
                            case Item.SocketGem.NormalThunderGem:
                            case Item.SocketGem.RefinedThunderGem:
                            case Item.SocketGem.SuperThunderGem:
                                break;
                            default:
                                await user.SendAsync(Language.StrNoGemEmbed);
                                return;
                        }
                    else if (main.GetItemSubType() == 202)
                        switch (gem)
                        {
                            case Item.SocketGem.NormalGloryGem:
                            case Item.SocketGem.RefinedGloryGem:
                            case Item.SocketGem.SuperGloryGem:
                                break;
                            default:
                                await user.SendAsync(Language.StrNoGemEmbed);
                                return;
                        }

                    if (Position == 1 || Position == 2 && main.SocketOne == Item.SocketGem.EmptySocket)
                    {
                        if (main.SocketOne == Item.SocketGem.NoSocket)
                        {
                            await user.SendAsync(Language.StrEmbedTargetNoSocket);
                            return;
                        }

                        if (main.SocketOne != Item.SocketGem.EmptySocket)
                        {
                            await user.SendAsync(Language.StrEmbedSocketAlreadyFilled);
                            return;
                        }

                        if (!await user.UserPackage.SpendItemAsync(minor))
                        {
                            await user.SendAsync(Language.StrEmbedNoRequiredItem);
                            return;
                        }

                        main.SocketOne = gem;
                        await main.SaveAsync();
                        break;
                    }

                    if (Position == 2)
                    {
                        if (main.SocketOne == Item.SocketGem.NoSocket || main.SocketOne == Item.SocketGem.EmptySocket)
                            return;

                        if (main.SocketTwo == Item.SocketGem.NoSocket)
                        {
                            await user.SendAsync(Language.StrEmbedNoSecondSocket);
                            return;
                        }

                        if (main.SocketTwo != Item.SocketGem.EmptySocket)
                        {
                            await user.SendAsync(Language.StrEmbedSocketAlreadyFilled);
                            return;
                        }

                        if (!await user.UserPackage.SpendItemAsync(minor))
                        {
                            await user.SendAsync(Language.StrEmbedNoRequiredItem);
                            return;
                        }

                        main.SocketTwo = gem;
                        await main.SaveAsync();
                    }

                    break;

                case EmbedAction.TakeOff:
                    if (Position == 1)
                    {
                        if (main.SocketOne == Item.SocketGem.NoSocket)
                            return;
                        if (main.SocketOne == Item.SocketGem.EmptySocket)
                            return;

                        main.SocketOne = Item.SocketGem.EmptySocket;

                        if (main.SocketTwo != Item.SocketGem.NoSocket && main.SocketTwo != Item.SocketGem.EmptySocket)
                        {
                            main.SocketOne = main.SocketTwo;
                            main.SocketTwo = Item.SocketGem.EmptySocket;
                        }

                        await main.SaveAsync();
                        break;
                    }

                    if (Position == 2)
                    {
                        if (main.SocketTwo == Item.SocketGem.NoSocket)
                            return;
                        if (main.SocketTwo == Item.SocketGem.EmptySocket)
                            return;

                        main.SocketTwo = Item.SocketGem.EmptySocket;
                        await main.SaveAsync();
                    }

                    break;
            }

            await user.SendAsync(new MsgItemInfo(main, MsgItemInfo<Client>.ItemMode.Update));
            await user.SendAsync(this);
        }
    }
}