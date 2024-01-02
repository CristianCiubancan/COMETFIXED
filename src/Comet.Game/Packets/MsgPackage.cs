using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.States.Npcs;
using Comet.Game.World.Managers;
using Comet.Game.World.Maps;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgPackage : MsgPackage<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;

            BaseNpc npc = null;
            Item storageItem = null;
            if (Mode == StorageType.Storage || Mode == StorageType.Trunk)
            {
                npc = RoleManager.GetRole(Identity) as BaseNpc;

                if (npc == null)
                {
                    if (user.IsPm())
                        await user.SendAsync($"Could not find Storage NPC, {Identity}");
                    return;
                }

                var interacting = RoleManager.GetRole<BaseNpc>(user.InteractingNpc);

                if (interacting == null || interacting.Type != BaseNpc.STORAGE_NPC) return;

                if (interacting.MapIdentity != 5000 &&
                    (interacting.MapIdentity != user.MapIdentity || interacting.GetDistance(user) > Screen.VIEW_SIZE))
                {
                    if (user.IsPm())
                        await user.SendAsync($"NPC not in range, {Identity}");
                    return;
                }

                if (interacting.MapIdentity == 5000)
                    switch (npc.MapIdentity)
                    {
                        case 1002: // twin
                        case 1036: // market
                            if (user.BaseVipLevel < 1)
                                return;
                            break;
                        case 1000: // desert
                            if (user.BaseVipLevel < 2)
                                return;
                            break;
                        case 1020: // canyon
                            if (user.BaseVipLevel < 3)
                                return;
                            break;
                        case 1015: // bird
                            if (user.BaseVipLevel < 4)
                                return;
                            break;
                        case 1011: // phoenix
                            if (user.BaseVipLevel < 5)
                                return;
                            break;
                        case 1213: // stone
                            if (user.BaseVipLevel < 6)
                                return;
                            break;
                    }
            }
            else if (Mode == StorageType.Chest)
            {
                storageItem = user.UserPackage[Identity];
                if (storageItem == null) return;
            }

            if (Action == WarehouseMode.Query)
            {
                foreach (Item item in user.UserPackage.GetStorageItems(Identity, Mode))
                {
                    Items.Add(new WarehouseItem
                    {
                        Identity = item.Identity,
                        Type = item.Type,
                        SocketOne = (byte) item.SocketOne,
                        SocketTwo = (byte) item.SocketTwo,
                        Blessing = (byte) item.Blessing,
                        Enchantment = item.Enchantment,
                        Magic1 = (byte) item.Effect,
                        Magic3 = item.Plus,
                        Locked = item.IsLocked(),
                        Color = (byte) item.Color,
                        Suspicious = false,
                        CompositionProgress = item.CompositionProgress,
                        SocketProgress = item.SocketProgress,
                        Bound = item.IsBound,
                        Inscribed = item.SyndicateIdentity != 0 ? 1 : 0
                    });

                    if (Items.Count >= 20)
                    {
                        await user.SendAsync(this);
                        Items.Clear();
                    }
                }

                if (Items.Count > 0)
                    await user.SendAsync(this);
            }
            else if (Action == WarehouseMode.CheckIn)
            {
                Item storeItem = user.UserPackage[Param];
                if (storeItem == null)
                {
                    await user.SendAsync(Language.StrItemNotFound);
                    return;
                }

                if (!storeItem.CanBeStored())
                {
                    await user.SendAsync(Language.StrItemCannotBeStored);
                    return;
                }

                if (Mode == StorageType.Storage && npc?.IsStorageNpc() != true)
                    return;
                if (Mode == StorageType.Chest && storageItem?.GetItemSort() != (Item.ItemSort?) 11)
                    return;

                if (user.UserPackage.StorageSize(Identity, Mode) >= 40) // all warehouses 40 blocks
                {
                    await user.SendAsync(Language.StrPackageFull);
                    return;
                }

                await user.UserPackage.AddToStorageAsync(Identity, storeItem, Mode, true);
            }
            else if (Action == WarehouseMode.CheckOut)
            {
                await user.UserPackage.GetFromStorageAsync(Identity, Param, Mode, true);
            }
        }
    }
}