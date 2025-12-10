using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApplication.Models.UserAccount
{
    [Table("USER_LOG")]
    public class UserLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("log_id")]
        public int LogId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("ip_address")]
        [MaxLength(50)]
        public string IpAddress { get; set; }

        [Column("login_time")]
        public DateTime LoginTime { get; set; }

        [Column("logout_time")]
        public DateTime? LogoutTime { get; set; }

        [Column("session_duration_minutes")]
        public int? SessionDurationMinutes { get; set; }

        [Column("user_agent")]
        [MaxLength(500)]
        public string UserAgent { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
