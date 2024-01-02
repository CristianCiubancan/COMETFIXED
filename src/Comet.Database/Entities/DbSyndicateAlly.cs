using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("cq_syn_ally")]
    public class DbSyndicateAlly
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("synid")] public virtual uint SyndicateIdentity { get; set; }
        [Column("synname")] public virtual string SyndicateName { get; set; }
        [Column("allyid")] public virtual uint AllyIdentity { get; set; }
        [Column("allyname")] public virtual string AllyName { get; set; }
        [Column("stabilish_date")] public virtual DateTime EstabilishDate { get; set; }
    }
}