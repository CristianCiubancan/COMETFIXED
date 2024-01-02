using System.Threading.Tasks;
using Comet.Core;
using Comet.Game.States;
using Comet.Game.States.Events;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using Comet.Shared;

namespace Comet.Game.Packets
{
    public sealed class MsgQualifyingInteractive : MsgQualifyingInteractive<Client>
    {
        public override async Task ProcessAsync(Client client)
        {
            Character user = RoleManager.GetUser(client.Character.Identity);
            if (user == null)
            {
                client.Disconnect();
                return;
            }

            var qualifier = EventManager.GetEvent<ArenaQualifier>();
            if (qualifier == null)
                return;

            switch (Interaction)
            {
                case InteractionType.Inscribe:
                {
                    if (user.CurrentEvent != null)
                    {
                        if (user.CurrentEvent is ArenaQualifier check && !check.IsInsideMatch(user.Identity))
                            await check.UnsubscribeAsync(user.Identity);
                        else
                            await user.SendAsync(Language.StrEventCannotEnterTwoEvents);
                        return;
                    }

                    if (qualifier.HasUser(user.Identity) && !qualifier.IsInsideMatch(user.Identity))
                    {
                        await qualifier.UnsubscribeAsync(user.Identity);
                        return;
                    }

                    await qualifier.InscribeAsync(user);
                    await ArenaQualifier.SendArenaInformationAsync(user);
                    await user.SendAsync(MsgQualifyingFightersList.CreateMsg());
                    return;
                }

                case InteractionType.Unsubscribe:
                {
                    await qualifier
                        .UnsubscribeAsync(
                            user.Identity); // no checks because user may be for some reason out of the event...
                    await ArenaQualifier.SendArenaInformationAsync(user);
                    await user.SendAsync(MsgQualifyingFightersList.CreateMsg());
                    return;
                }

                case InteractionType.Accept:
                {
                    ArenaQualifier.QualifierMatch match = qualifier.FindMatch(user.Identity);
                    if (match == null)
                    {
                        await qualifier.UnsubscribeAsync(user.Identity);
                        return;
                    }

                    if (match.InvitationExpired)
                        // do nothing, because thread may remove with defeat for default
                        return;

                    if (Option == 1)
                    {
                        if (match.Player1.Identity == user.Identity)
                        {
                            if (match.Accepted1)
                                return;

                            match.Accepted1 = true;
                        }
                        else if (match.Player2.Identity == user.Identity)
                        {
                            if (match.Accepted2)
                                return;

                            match.Accepted2 = true;
                        }

                        if (match.Accepted1 && match.Accepted2) await match.StartAsync();
                    }
                    else
                    {
                        await match.FinishAsync(null, user);
                    }

                    return;
                }

                case InteractionType.GiveUp:
                {
                    ArenaQualifier.QualifierMatch match = qualifier.FindMatchByMap(user.MapIdentity);
                    if (match == null ||
                        !match.IsRunning) // check if running, because if other player gave up first it may not happen twice
                    {
                        await qualifier.UnsubscribeAsync(user.Identity);
                        return;
                    }

                    await match.FinishAsync(null, user);
                    await ArenaQualifier.SendArenaInformationAsync(user);
                    await user.SendAsync(MsgQualifyingFightersList.CreateMsg());
                    return;
                }

                case InteractionType.BuyArenaPoints:
                {
                    if (user.QualifierPoints > 0)
                        return;

                    if (!await user.SpendMoneyAsync(ArenaQualifier.PRICE_PER_1500_POINTS, true))
                        return;

                    user.QualifierPoints += 1500;
                    await ArenaQualifier.SendArenaInformationAsync(user);
                    await user.SendAsync(MsgQualifyingFightersList.CreateMsg());
                    return;
                }

                case InteractionType.ReJoin:
                {
                    await qualifier.UnsubscribeAsync(user.Identity);
                    await qualifier.InscribeAsync(user);
                    await ArenaQualifier.SendArenaInformationAsync(user);
                    await user.SendAsync(MsgQualifyingFightersList.CreateMsg());
                    return;
                }

                default:
                {
                    await client.SendAsync(this);
                    if (client.Character.IsPm())
                        await client.SendAsync(new MsgTalk(client.Identity, TalkChannel.Service,
                                                           $"Missing packet {Type}, Action {Interaction}, Length {Length}"));

                    await Log.WriteLogAsync(LogLevel.Warning,
                                            "Missing packet {0}, Action {1}, Length {2}\n{3}",
                                            Type, Interaction, Length, PacketDump.Hex(Encode()));
                    break;
                }
            }
        }
    }
}