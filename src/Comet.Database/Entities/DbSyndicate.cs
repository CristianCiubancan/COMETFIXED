using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("cq_syndicate")]
    public class DbSyndicate
    {
        [Key] [Column("id")] public virtual ushort Identity { get; set; }
        [Column("creation_date")] public virtual DateTime CreationDate { get; set; }
        [Column("name")] public virtual string Name { get; set; }
        [Column("announce")] public virtual string Announce { get; set; }
        [Column("announce_date")] public virtual DateTime AnnounceDate { get; set; }
        [Column("leader_id")] public virtual uint LeaderIdentity { get; set; }
        [Column("leader_name")] public virtual string LeaderName { get; set; }
        [Column("money")] public virtual long Money { get; set; }
        [Column("emoney")] public virtual uint ConquerPoints { get; set; }
        [Column("del_flag")] public virtual DateTime? DelFlag { get; set; }
        [Column("amount")] public virtual uint Amount { get; set; }
        [Column("totem_pole")] public virtual int TotemPole { get; set; }
        [Column("last_totem")] public virtual DateTime? LastTotem { get; set; }
        [Column("condition_level")] public virtual byte ReqLevel { get; set; }
        [Column("condition_prof")] public virtual uint ReqClass { get; set; }
        [Column("condition_metem")] public virtual byte ReqMetempsychosis { get; set; }
        [Column("synrank")] public virtual byte Level { get; set; }
    }
}