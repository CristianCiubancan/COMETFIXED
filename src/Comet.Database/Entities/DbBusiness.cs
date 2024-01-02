using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("cq_business")]
    public class DbBusiness
    {
        [Key] [Column("id")] public virtual uint Identity { get; set; }
        [Column("userid")] public virtual uint UserId { get; set; }
        [Column("business")] public virtual uint BusinessId { get; set; }
        [Column("date")] public virtual DateTime Date { get; set; }

        public virtual DbCharacter User { get; set; }
        public virtual DbCharacter Business { get; set; }
    }
}