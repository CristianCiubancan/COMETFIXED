using Comet.Game.States;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1006</remarks>
    /// <summary>
    ///     Message defining character information, used to initialize the client interface
    ///     and game state. Character information is loaded from the game database on login
    ///     if a character exists.
    /// </summary>
    public sealed class MsgUserInfo : MsgUserInfo<Client>
    {
        /// <summary>
        ///     Instantiates a new instance of <see cref="MsgUserInfo" /> using data fetched
        ///     from the database and stored in <see cref="DbCharacter" />.
        /// </summary>
        /// <param name="character">Character info from the database</param>
        public MsgUserInfo(Character character)
        {
            Type = PacketType.MsgUserInfo;
            Identity = character.Identity;
            Mesh = character.Mesh;
            Hairstyle = character.Hairstyle;
            Silver = character.Silvers;
            Jewels = character.ConquerPoints;
            Experience = character.Experience;
            Strength = character.Strength;
            Agility = character.Agility;
            Vitality = character.Vitality;
            Spirit = character.Spirit;
            AttributePoints = character.AttributePoints;
            HealthPoints = (ushort) character.Life;
            ManaPoints = (ushort) character.Mana;
            KillPoints = character.PkPoints;
            Level = character.Level;
            CurrentClass = character.Profession;
            PreviousClass = character.PreviousProfession;
            FirstClass = character.PreviousProfession;
            Rebirths = character.Metempsychosis;
            QuizPoints = character.QuizPoints;
            EnlightenPoints = (ushort) character.EnlightenPoints;
            EnlightenExp = character.EnlightenExperience / (Character.ENLIGHTENMENT_UPLEV_MAX_EXP / 2);
            VipLevel = character.BaseVipLevel;
            UserTitle = character.UserTitle;
            CharacterName = character.Name;
            SpouseName = character.MateName;
        }
    }
}