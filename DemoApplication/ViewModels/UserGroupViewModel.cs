using System.ComponentModel.DataAnnotations;

namespace DemoApplication.ViewModels
{
    public class UserGroupViewModel
    {
        public int UserGroupId { get; set; }

        [Required(ErrorMessage = "Enter User Group Name")]
        [Display(Name = "User Group Name")]
        [MaxLength(100)]
        public string UserGroupName { get; set; }
    }
}
