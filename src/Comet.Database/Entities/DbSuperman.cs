using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("cq_superman")]
    public class DbSuperman
    {
        [Key] [Column("id")] public uint Identity { get; set; }

        [Column("user_id")] public uint UserIdentity { get; set; }
        [Column("number")] public uint Amount { get; set; }
    }
}