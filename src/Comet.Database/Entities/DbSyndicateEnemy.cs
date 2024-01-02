using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("cq_syn_enemy")]
    public class DbSyndicateEnemy
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("synid")] public virtual uint SyndicateIdentity { get; set; }
        [Column("synname")] public virtual string SyndicateName { get; set; }
        [Column("enemyid")] public virtual uint EnemyIdentity { get; set; }
        [Column("enemyname")] public virtual string EnemyName { get; set; }
        [Column("stabilish_date")] public virtual DateTime EstabilishDate { get; set; }
    }
}