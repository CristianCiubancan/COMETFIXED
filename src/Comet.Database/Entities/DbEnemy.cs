using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("cq_enemy")]
    public class DbEnemy
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }

        [Column("userid")] public virtual uint UserIdentity { get; set; }

        [Column("enemy")] public virtual uint TargetIdentity { get; set; }

        [Column("time")] public virtual DateTime Time { get; set; }
    }
}