using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApplication.Models.UserAccount
{
    [Table("FEEDBACK")]
    public class Feedback
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("feedback_id")]
        public int FeedbackId { get; set; }

        [Column("user_id")]
        public int UserId { get; set; }

        [Column("subject")]
        [Required(ErrorMessage = "Subject is required")]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Column("message")]
        [Required(ErrorMessage = "Message is required")]
        [MaxLength(2000)]
        public string Message { get; set; }

        [Column("image_path")]
        [MaxLength(500)]
        public string ImagePath { get; set; }

        [Column("created_date")]
        public DateTime CreatedDate { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; } = false;

        [Column("admin_notes")]
        [MaxLength(1000)]
        public string? AdminNotes { get; set; }

        // Navigation property
        [ForeignKey("UserId")]
        public virtual User User { get; set; }
    }
}
