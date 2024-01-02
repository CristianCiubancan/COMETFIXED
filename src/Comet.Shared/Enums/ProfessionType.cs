namespace Comet.Shared.Enums
{
    /// <summary>
    ///     This enumeration type defines the possible professions for a character in Conquer Online, defined by the
    ///     client's "ProfessionalName.ini" file.
    /// </summary>
    public enum ProfessionType : ushort
    {
        None = 0,

        // Trojan Professions:
        InternTrojan = 10,
        Trojan = 11,
        VeteranTrojan = 12,
        TigerTrojan = 13,
        DragonTrojan = 14,
        TrojanMaster = 15,

        // Warrior Professions:
        InternWarrior = 20,
        Warrior = 21,
        BrassWarrior = 22,
        SilverWarrior = 23,
        GoldWarrior = 24,
        WarriorMaster = 25,

        // Archer Professions:
        InternArcher = 40,
        Archer = 41,
        EagleArcher = 42,
        TigerArcher = 43,
        DragonArcher = 44,
        ArcherMaster = 45,

        // Ninja Profession:
        InternNinja = 50,
        Ninja = 51,
        MiddleNinja = 52,
        DarkNinja = 53,
        MysticNinja = 54,
        NinjaMaster = 55,

        //Monk Profession:
        InternMonk = 60,
        Monk = 61,
        DhyanaMonk = 62,
        DharmaMonk = 63,
        PrajnaMonk = 64,
        NirvanaMonk = 65,

        // Pirate Profession:
        InternPirate = 70,
        Pirate = 71,
        GunnerPirate = 72,
        QuarterPirate = 73,
        CaptainPirate = 74,
        LordPirate = 75,

        // Taoist Professions:
        InternTaoist = 100,
        Taoist = 101,
        WaterTaoist = 132,
        WaterWizard = 133,
        WaterMaster = 134,
        WaterSaint = 135,
        FireTaoist = 142,
        FireWizard = 143,
        FireMaster = 144,
        FireSaint = 145,

        Error = 255
    }
}
