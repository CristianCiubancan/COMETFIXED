namespace Comet.Shared.Enums
{
    public enum ItemPosition : ushort
    {
        Inventory = 0,
        Headgear = 1,
        EquipmentBegin = 1,
        Necklace = 2,
        Armor = 3,
        RightHand = 4,
        LeftHand = 5,
        Ring = 6,
        Gourd = 7,
        Boots = 8,
        Garment = 9,
        AttackTalisman = 10,
        DefenseTalisman = 11,
        Steed = 12,
        RightHandAccessory = 15,
        LeftHandAccessory = 16,
        SteedArmor = 17,
        Crop = 18,
        EquipmentEnd = Crop,

        AltHead = 21,
        AltNecklace = 22,
        AltArmor = 23,
        AltWeaponR = 24,
        AltWeaponL = 25,
        AltRing = 26,
        AltBottle = 27,
        AltBoots = 28,
        AltGarment = 29,
        AltFan = 30,
        AltTower = 31,
        AltSteed = 32,

        UserLimit = 199,

        /// <summary>
        ///     Warehouse
        /// </summary>
        Storage = 201,

        /// <summary>
        ///     House WH
        /// </summary>
        Trunk = 202,

        /// <summary>
        ///     Sashes
        /// </summary>
        Chest = 203,

        Detained = 250,
        Floor = 254
    }
}
