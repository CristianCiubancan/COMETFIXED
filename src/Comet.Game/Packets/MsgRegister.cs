using System;
using System.Threading.Tasks;
using Comet.Database.Entities;
using Comet.Game.Database;
using Comet.Game.Database.Repositories;
using Comet.Game.States;
using Comet.Game.States.Items;
using Comet.Game.World.Managers;
using Comet.Network.Packets;
using Comet.Network.Packets.Game;
using static Comet.Game.Packets.MsgTalk;

namespace Comet.Game.Packets
{
    /// <remarks>Packet Type 1001</remarks>
    /// <summary>
    ///     Message containing character creation details, such as the new character's name,
    ///     body size, and profession. The character name should be verified, and may be
    ///     rejected by the server if a character by that name already exists.
    /// </summary>
    public sealed class MsgRegister : MsgRegister<Client>
    {
        private static readonly ushort[] m_startX = {430, 423, 439, 428, 452, 464, 439};
        private static readonly ushort[] m_startY = {378, 394, 384, 365, 365, 378, 396};

        // Registration constants
        private static readonly byte[] Hairstyles =
        {
            10, 11, 13, 14, 15, 24, 30, 35, 37, 38, 39, 40
        };

        /// <summary>
        ///     Process can be invoked by a packet after decode has been called to structure
        ///     packet fields and properties. For the server implementations, this is called
        ///     in the packet handler after the message has been dequeued from the server's
        ///     <see cref="PacketProcessor{TClient}" />.
        /// </summary>
        /// <param name="client">Client requesting packet processing</param>
        public override async Task ProcessAsync(Client client)
        {
            // Validate that the player has access to character creation
            if (client.Creation == null || Token != client.Creation.Token ||
                !Kernel.Registration.Contains(Token))
            {
                await client.SendAsync(RegisterInvalid);
                client.Disconnect();
                return;
            }

            // Check character name availability
            if (await CharactersRepository.ExistsAsync(CharacterName))
            {
                await client.SendAsync(RegisterNameTaken);
                return;
            }

            if (!Kernel.IsValidName(CharacterName))
            {
                await client.SendAsync(RegisterInvalid);
                return;
            }

            // Validate character creation input
            if (!Enum.IsDefined(typeof(BodyType), Mesh) ||
                !Enum.IsDefined(typeof(BaseClassType), Class))
            {
                await client.SendAsync(RegisterInvalid);
                return;
            }

            DbPointAllot allot = ExperienceManager.GetPointAllot((ushort) (Class / 10), 1) ?? new DbPointAllot
            {
                Strength = 4,
                Agility = 6,
                Vitality = 12,
                Spirit = 0
            };

            // Create the character
            var character = new DbCharacter
            {
                AccountIdentity = client.Creation.AccountID,
                Name = CharacterName,
                Mate = 0,
                Profession = (byte) Class,
                Mesh = Mesh,
                Silver = 1000,
                Level = 1,
                MapID = 1002,
                X = m_startX[await Kernel.NextAsync(m_startX.Length) % m_startX.Length],
                Y = m_startY[await Kernel.NextAsync(m_startY.Length) % m_startY.Length],
                Strength = allot.Strength,
                Agility = allot.Agility,
                Vitality = allot.Vitality,
                Spirit = allot.Spirit,
                HealthPoints =
                    (ushort) (allot.Strength * 3
                              + allot.Agility * 3
                              + allot.Spirit * 3
                              + allot.Vitality * 24),
                ManaPoints = (ushort) (allot.Spirit * 5),
                Registered = DateTime.Now,
                ExperienceMultiplier = 5,
                ExperienceExpires = DateTime.Now.AddHours(1),
                HeavenBlessing = DateTime.Now.AddDays(30),
                AutoAllot = 1
            };

            // Generate a random look for the character
            var body = (BodyType) Mesh;
            switch (body)
            {
                case BodyType.AgileFemale:
                case BodyType.MuscularFemale:
                    character.Mesh += 2010000;
                    break;
                default:
                    character.Mesh += 10000;
                    break;
            }

            character.Hairstyle = (ushort) (
                                               await Kernel.NextAsync(3, 9) * 100 + Hairstyles[
                                                   await Kernel.NextAsync(0, Hairstyles.Length)]);

            try
            {
                // Save the character and continue with login
                await CharactersRepository.CreateAsync(character);
                Kernel.Registration.Remove(client.Creation.Token);
                await GenerateInitialEquipmentAsync(character);
            }
            catch
            {
                await client.SendAsync(RegisterTryAgain);
                return;
            }

            await client.SendAsync(RegisterOk);
        }

        private async Task GenerateInitialEquipmentAsync(DbCharacter user)
        {
            DbNewbieInfo info = await NewbieInfoRepository.GetAsync(user.Profession);
            if (info == null)
                return;

            if (info.LeftHand != 0)
                await CreateItemAsync(info.LeftHand, user.Identity, Item.ItemPosition.LeftHand);
            if (info.RightHand != 0)
                await CreateItemAsync(info.RightHand, user.Identity, Item.ItemPosition.RightHand);
            if (info.Shoes != 0)
                await CreateItemAsync(info.Shoes, user.Identity, Item.ItemPosition.Boots);
            if (info.Headgear != 0)
                await CreateItemAsync(info.Headgear, user.Identity, Item.ItemPosition.Headwear);
            if (info.Necklace != 0)
                await CreateItemAsync(info.Necklace, user.Identity, Item.ItemPosition.Necklace);
            if (info.Armor != 0)
                await CreateItemAsync(info.Armor, user.Identity, Item.ItemPosition.Armor);
            if (info.Ring != 0)
                await CreateItemAsync(info.Ring, user.Identity, Item.ItemPosition.Ring);

            if (info.Item0 != 0)
                for (var i = 0; i < info.Number0; i++)
                    await CreateItemAsync(info.Item0, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item1 != 0)
                for (var i = 0; i < info.Number1; i++)
                    await CreateItemAsync(info.Item1, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item2 != 0)
                for (var i = 0; i < info.Number2; i++)
                    await CreateItemAsync(info.Item2, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item3 != 0)
                for (var i = 0; i < info.Number3; i++)
                    await CreateItemAsync(info.Item3, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item4 != 0)
                for (var i = 0; i < info.Number4; i++)
                    await CreateItemAsync(info.Item4, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item5 != 0)
                for (var i = 0; i < info.Number5; i++)
                    await CreateItemAsync(info.Item5, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item6 != 0)
                for (var i = 0; i < info.Number6; i++)
                    await CreateItemAsync(info.Item6, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item7 != 0)
                for (var i = 0; i < info.Number7; i++)
                    await CreateItemAsync(info.Item7, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item8 != 0)
                for (var i = 0; i < info.Number8; i++)
                    await CreateItemAsync(info.Item8, user.Identity, Item.ItemPosition.Inventory);

            if (info.Item9 != 0)
                for (var i = 0; i < info.Number9; i++)
                    await CreateItemAsync(info.Item9, user.Identity, Item.ItemPosition.Inventory);

            if (info.Magic0 != 0)
                await CreateMagicAsync(user.Identity, (ushort) info.Magic0);
            if (info.Magic1 != 0)
                await CreateMagicAsync(user.Identity, (ushort) info.Magic1);
            if (info.Magic2 != 0)
                await CreateMagicAsync(user.Identity, (ushort) info.Magic2);
            if (info.Magic3 != 0)
                await CreateMagicAsync(user.Identity, (ushort) info.Magic3);
        }

        private async Task CreateItemAsync(uint type, uint idOwner, Item.ItemPosition position, byte add = 0,
                                           Item.SocketGem gem1 = Item.SocketGem.NoSocket,
                                           Item.SocketGem gem2 = Item.SocketGem.NoSocket,
                                           byte enchant = 0, byte reduceDmg = 0)
        {
            DbItem item = Item.CreateEntity(type);
            if (item == null)
                return;
            item.Position = (byte) position;
            item.PlayerId = idOwner;
            item.AddLife = enchant;
            item.ReduceDmg = reduceDmg;
            item.Magic3 = add;
            item.Gem1 = (byte) gem1;
            item.Gem2 = (byte) gem2;
            await ServerDbContext.SaveAsync(item);
        }

        private Task CreateMagicAsync(uint idOwner, ushort type, byte level = 0)
        {
            return ServerDbContext.SaveAsync(new DbMagic
            {
                Type = type,
                Level = level,
                OwnerId = idOwner
            });
        }
    }
}