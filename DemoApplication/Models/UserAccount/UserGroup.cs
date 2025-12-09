using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DemoApplication.Models.UserAccount
{
    [Table("LKP_USER_GROUP")]
    public class LkpUserGroup
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("user_group_id")]
        public int UserGroupId { get; set; }

        [Column("user_group_name")]
        [Required(ErrorMessage = "Enter User Group Name")]
        [MaxLength(100)]
        public string UserGroupName { get; set; }

        // Navigation property
        public virtual ICollection<User> Users { get; set; }
    }
}
