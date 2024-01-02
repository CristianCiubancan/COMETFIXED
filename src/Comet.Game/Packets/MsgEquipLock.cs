using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgEquipLock : MsgEquipLock<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Item item = client.Character.UserPackage.FindByIdentity(Identity);
            if (item == null)
            {
                await client.Character.SendAsync(Language.StrItemNotFound);
                return;
            }

            switch (Action)
            {
                case LockMode.RequestLock:
                {
                    if (item.IsLocked() && !item.IsUnlocking())
                    {
                        await client.Character.SendAsync(Language.StrEquipLockAlreadyLocked);
                        return;
                    }

                    if (!item.IsEquipment() && !item.IsMount())
                    {
                        await client.Character.SendAsync(Language.StrEquipLockCantLock);
                        return;
                    }

                    await item.SetLockAsync();
                    await client.SendAsync(this);
                    await client.SendAsync(new MsgItemInfo(item, MsgItemInfo<Client>.ItemMode.Update));
                    break;
                }

                case LockMode.RequestUnlock:
                {
                    if (!item.IsLocked())
                    {
                        await client.Character.SendAsync(Language.StrEquipLockNotLocked);
                        return;
                    }

                    if (item.IsUnlocking())
                    {
                        await client.Character.SendAsync(Language.StrEquipLockAlreadyUnlocking);
                        return;
                    }

                    await item.SetUnlockAsync();
                    await client.SendAsync(new MsgItemInfo(item, MsgItemInfo<Client>.ItemMode.Update));
                    await item.TryUnlockAsync();
                    break;
                }
            }
        }
    }
}