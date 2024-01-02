using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.World.Managers;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgFriend : MsgFriend<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = client.Character;
            Character target = null;
            switch (Action)
            {
                case MsgFriendAction.RequestFriend:
                    target = RoleManager.GetUser(Identity);
                    if (target == null)
                    {
                        await user.SendAsync(Language.StrTargetNotOnline);
                        return;
                    }

                    if (user.FriendAmount >= user.MaxFriendAmount)
                    {
                        await user.SendAsync(Language.StrFriendListFull);
                        return;
                    }

                    if (target.FriendAmount >= target.MaxFriendAmount)
                    {
                        await user.SendAsync(Language.StrTargetFriendListFull);
                        return;
                    }

                    uint request = target.QueryRequest(RequestType.Friend);
                    if (request == user.Identity)
                    {
                        target.PopRequest(RequestType.Friend);
                        await target.CreateFriendAsync(user);
                    }
                    else
                    {
                        user.SetRequest(RequestType.Friend, target.Identity);
                        await target.SendAsync(new MsgFriend
                        {
                            Action = MsgFriendAction.RequestFriend,
                            Identity = user.Identity,
                            Name = user.Name
                        });
                        await user.SendAsync(Language.StrMakeFriendSent);
                    }

                    break;

                case MsgFriendAction.RemoveFriend:
                    await user.DeleteFriendAsync(Identity, true);
                    break;

                case MsgFriendAction.RemoveEnemy:
                    await user.DeleteEnemyAsync(Identity);
                    break;
            }
        }
    }
}