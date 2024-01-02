using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("family_attr")]
    public class DbFamilyAttr
    {
        [Key] [Column("id")] public uint Identity { get; set; }
        [Column("user_id")] public uint UserIdentity { get; set; }
        [Column("family_id")] public uint FamilyIdentity { get; set; }
        [Column("rank")] public byte Rank { get; set; }
        [Column("proffer")] public uint Proffer { get; set; }
        [Column("join_date")] public DateTime JoinDate { get; set; }
        [Column("auto_exercise")] public byte AutoExercise { get; set; }
        [Column("exp_date")] public uint ExpDate { get; set; }
    }
}