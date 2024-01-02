using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("cq_monster_type_magic")]
    public class DbMonsterTypeMagic
    {
        [Key] [Column("id")] public uint Id { get; set; }

        [Column("monstertype")] public uint MonsterType { get; set; }

        [Column("magic_type")] public uint MagicType { get; set; }

        [Column("magic_lev")] public uint MagicLev { get; set; }

        [Column("cold_time")] public uint ColdTime { get; set; }

        [Column("warning_time")] public ushort WarningTime { get; set; }

        [Column("status_magic_type")] public uint StatusMagicType { get; set; }

        [Column("status_magic_lev")] public uint StatusMagicLev { get; set; }
    }
}