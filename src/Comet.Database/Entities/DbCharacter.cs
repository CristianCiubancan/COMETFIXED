using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Comet.Database.Entities
{
    /// <summary>
    ///     Character information associated with a player. Every player account is permitted
    ///     a single character on the server. Contains the character's defining look and features,
    ///     level and attribute information, location, etc.
    /// </summary>
    [Table("cq_user")]
    public class DbCharacter
    {
        // Column Properties
        [Key] [Column("id")] public virtual uint Identity { get; set; }

        [Column("account_id")] public virtual uint AccountIdentity { get; set; }
        [Column("name")] public virtual string Name { get; set; }
        [Column("mateid")] public virtual uint Mate { get; set; }
        [Column("lookface")] public virtual uint Mesh { get; set; }
        [Column("hair")] public virtual ushort Hairstyle { get; set; }
        [Column("money")] public virtual uint Silver { get; set; }
        [Column("emoney")] public virtual uint ConquerPoints { get; set; }
        [Column("money_saved")] public virtual uint StorageMoney { get; set; }
        [Column("profession")] public virtual byte Profession { get; set; }
        [Column("old_prof")] public virtual byte PreviousProfession { get; set; }
        [Column("first_prof")] public virtual byte FirstProfession { get; set; }
        [Column("metempsychosis")] public virtual byte Rebirths { get; set; }
        [Column("level")] public virtual byte Level { get; set; }
        [Column("exp")] public virtual ulong Experience { get; set; }
        [Column("recordmap_id")] public virtual uint MapID { get; set; }
        [Column("recordx")] public virtual ushort X { get; set; }
        [Column("recordy")] public virtual ushort Y { get; set; }
        [Column("virtue")] public virtual uint Virtue { get; set; }
        [Column("strength")] public virtual ushort Strength { get; set; }
        [Column("speed")] public virtual ushort Agility { get; set; }
        [Column("health")] public virtual ushort Vitality { get; set; }
        [Column("soul")] public virtual ushort Spirit { get; set; }
        [Column("additional_point")] public virtual ushort AttributePoints { get; set; }
        [Column("life")] public virtual ushort HealthPoints { get; set; }
        [Column("mana")] public virtual ushort ManaPoints { get; set; }
        [Column("pk")] public virtual ushort KillPoints { get; set; }
        [Column("creation_date")] public virtual DateTime? Registered { get; set; }
        [Column("donation")] public ulong Donation { get; set; }
        [Column("last_login")] public virtual DateTime LoginTime { get; set; }
        [Column("last_logout")] public virtual DateTime LogoutTime { get; set; }
        [Column("last_logout2")] public virtual DateTime? LogoutTime2 { get; set; } // Offline TG
        [Column("online_time")] public virtual int OnlineSeconds { get; set; }
        [Column("auto_allot")] public virtual byte AutoAllot { get; set; }
        [Column("mete_lev")] public virtual uint MeteLevel { get; set; }
        [Column("exp_ball_usage")] public virtual uint ExpBallUsage { get; set; }
        [Column("exp_ball_num")] public virtual uint ExpBallNum { get; set; }
        [Column("exp_multiply")] public virtual float ExperienceMultiplier { get; set; }
        [Column("exp_expires")] public virtual DateTime? ExperienceExpires { get; set; }
        [Column("god_status")] public virtual DateTime? HeavenBlessing { get; set; }
        [Column("task_mask")] public virtual uint TaskMask { get; set; }
        [Column("home_id")] public virtual uint HomeIdentity { get; set; }
        [Column("lock_key")] public virtual ulong LockKey { get; set; }
        [Column("auto_exercise")] public virtual ushort AutoExercise { get; set; }
        [Column("time_of_life")] public virtual DateTime? LuckyTime { get; set; }
        [Column("vip_value")] public virtual uint VipLevel { get; set; }
        [Column("vip_expire")] public virtual DateTime? VipExpiration { get; set; }
        [Column("business")] public virtual DateTime? Business { get; set; }
        [Column("send_flower_date")] public DateTime? SendFlowerDate { get; set; }
        [Column("flower_r")] public uint FlowerRed { get; set; }
        [Column("flower_w")] public uint FlowerWhite { get; set; }
        [Column("flower_lily")] public uint FlowerOrchid { get; set; }
        [Column("flower_tulip")] public uint FlowerTulip { get; set; }

        /// <summary>
        ///     Experience Gained by staying online with Heaven Blessing.
        /// </summary>
        [Column("online_god_exptime")]
        public uint OnlineGodExpTime { get; set; }

        /// <summary>
        ///     Experience gained by killing monsters in the world with Heaven Blessing.
        /// </summary>
        [Column("battle_god_exptime")]
        public uint BattleGodExpTime { get; set; }

        /// <summary>
        ///     Amount of times remaining to enlight other player.
        /// </summary>
        [Column("mentor_opportunity")]
        public uint MentorOpportunity { get; set; }

        /// <summary>
        ///     Enlightment experience to be awarded.
        /// </summary>
        [Column("mentor_uplev_time")]
        public uint MentorUplevTime { get; set; }

        /// <summary>
        ///     Amount of times enlightened.
        /// </summary>
        [Column("mentor_achieve")]
        public uint MentorAchieve { get; set; }

        /// <summary>
        ///     Enlightment last reset time.
        /// </summary>
        [Column("mentor_day")]
        public uint MentorDay { get; set; }

        [Column("title")] public uint Title { get; set; }
        [Column("title_select")] public byte TitleSelect { get; set; }

        [Column("athlete_point")] public uint AthletePoint { get; set; }
        [Column("athlete_history_wins")] public uint AthleteHistoryWins { get; set; }
        [Column("athlete_history_loses")] public uint AthleteHistoryLoses { get; set; }
        [Column("athlete_day_wins")] public uint AthleteDayWins { get; set; }
        [Column("athlete_day_loses")] public uint AthleteDayLoses { get; set; }
        [Column("athlete_cur_honor_point")] public uint AthleteCurrentHonorPoints { get; set; }

        [Column("athlete_hisorty_honor_point")]
        public uint AthleteHistoryHonorPoints { get; set; }

        [Column("emoney_mono")] public uint ConquerPointsBound { get; set; }

        [Column("quiz_point")] public uint QuizPoints { get; set; }

        [Column("nationality")] public ushort Nationality { get; set; }
        [Column("cultivation")] public uint Cultivation { get; set; }      // Study Points
        [Column("strength_value")] public uint StrengthValue { get; set; } // ChiPoints
        [Column("day_reset_date")] public uint DayResetDate { get; set; }
    }
}