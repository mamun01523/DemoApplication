using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApplication.Models.UserAccount
{
    [Table("USER")]
    public class User
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("user_id")]
        public int UserId { get; set; }

        [Column("full_name")]
        [Required(ErrorMessage = "Enter Full Name")]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Column("user_name")]
        [Required(ErrorMessage = "Enter Username")]
        [MaxLength(50)]
        public string Username { get; set; }

        [Column("email")]
        [Required(ErrorMessage = "Enter email address")]
        [EmailAddress(ErrorMessage = "Enter valid email address")]
        [MaxLength(100)]
        public string Email { get; set; }

        [Column("phone_no")]
        [StringLength(11, ErrorMessage = "Enter 11 digit phone no. e.g. 01686xxxxxx")]
        public string PhoneNo { get; set; }

        [Column("user_group_id")]
        [Required(ErrorMessage = "Select User Group")]
        public int UserGroupId { get; set; }

        [Column("password_hash")]
        [Required]
        public string PasswordHash { get; set; }

        [Column("password_salt")]
        public string PasswordSalt { get; set; }

        [Column("reset_token")]
        public string? ResetToken { get; set; }

        [Column("reset_token_expiry")]
        public DateTime? ResetTokenExpiry { get; set; }

        [Column("created_date")]
        public DateTime? CreatedDate { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; } = true;

        // Navigation property
        [ForeignKey("UserGroupId")]
        public virtual LkpUserGroup UserGroup { get; set; }
    }
}
