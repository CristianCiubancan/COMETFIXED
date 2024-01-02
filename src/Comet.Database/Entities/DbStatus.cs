using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    [Table("cq_status")]
    public class DbStatus
    {
        [Key] [Column("id")] public virtual uint Id { get; set; }
        [Column("owner_id")] public virtual uint OwnerId { get; set; }
        [Column("status")] public virtual uint Status { get; set; }
        [Column("power")] public virtual int Power { get; set; }
        [Column("sort")] public virtual uint Sort { get; set; }
        [Column("leave_times")] public virtual uint LeaveTimes { get; set; }
        [Column("remain_time")] public virtual uint RemainTime { get; set; }
        [Column("end_time")] public virtual DateTime EndTime { get; set; }
        [Column("interval_time")] public virtual uint IntervalTime { get; set; }
    }
}