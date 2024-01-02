using System;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    public sealed class MsgPlayer : MsgPlayer<Client>
    {
        public MsgPlayer(Character user, Character target, ushort x = 0, ushort y = 0)
        {
            Identity = user.Identity;
            Mesh = user.Mesh;

            MapX = x == 0 ? user.MapX : x;
            MapY = y == 0 ? user.MapY : y;

            Status = user.StatusFlag;

            Hairstyle = user.Hairstyle;
            Direction = (byte) user.Direction;
            Pose = (ushort) user.Action;
            Metempsychosis = user.Metempsychosis;
            Level = user.Level;

            SyndicateIdentity = user.SyndicateIdentity;
            SyndicatePosition = (ushort) user.SyndicateRank;

            NobilityRank = (uint) user.NobilityRank;

            Helmet = user.Headgear?.Type ?? 0;
            HelmetColor = (ushort) (user.Headgear?.Color ?? Item.ItemColor.None);
            RightHand = user.RightHand?.Type ?? 0;
            LeftHand = user.LeftHand?.Type ?? 0;
            LeftHandColor = (ushort) (user.LeftHand?.Color ?? Item.ItemColor.None);
            Armor = user.Armor?.Type ?? 0;
            ArmorColor = (ushort) (user.Armor?.Color ?? Item.ItemColor.None);
            Garment = user.Garment?.Type ?? 0;

            Mount = user.Mount?.Type ?? 0;
            MountExperience = (int) (user.Mount?.CompositionProgress ?? 0);
            MountAddition = user.Mount?.Plus ?? 0;
            MountColor = user.Mount?.SocketProgress ?? 0;

            FlowerRanking = user.FlowerCharm;
            QuizPoints = user.QuizPoints;
            UserTitle = user.UserTitle;

            EnlightenPoints = (ushort) (target.CanBeEnlightened(user) ? user.EnlightenPoints : 0);
            CanBeEnlightened = user.CanBeEnlightened(target);

            Away = user.IsAway;

            if (user.Syndicate != null)
                SharedBattlePower = (uint) user.Syndicate.GetSharedBattlePower(user.SyndicateRank);

            TotemBattlePower = user.Syndicate?.TotemSharedBattlePower ?? 0;

            FamilyIdentity = user.FamilyIdentity;
            FamilyRank = (uint) user.FamilyPosition;
            FamilyBattlePower = user.FamilyBattlePower;

            Name = user.Name;
            FamilyName = user.FamilyName;
        }

        public MsgPlayer(Monster monster, ushort x = 0, ushort y = 0)
        {
            Type = PacketType.MsgPlayer;

            Identity = monster.Identity;

            Mesh = monster.Mesh;

            MapX = x == 0 ? monster.MapX : x;
            MapY = y == 0 ? monster.MapY : y;

            Status = (uint) monster.StatusFlag;

            Direction = (byte) monster.Direction;
            Pose = (ushort) monster.Action;

            MonsterLevel = monster.Level;
            if (monster.Life > ushort.MaxValue)
                MonsterLife = (ushort) (ushort.MaxValue / monster.Life * 100);
            else
                MonsterLife = (ushort) Math.Min(ushort.MaxValue, monster.Life);

            Name = monster.Name;
            FamilyName = "";
        }
    }
}