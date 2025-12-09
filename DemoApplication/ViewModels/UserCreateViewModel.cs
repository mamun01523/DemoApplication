using DemoApplication.Models.UserAccount;
using System.ComponentModel.DataAnnotations;

namespace DemoApplication.ViewModels
{
    public class UserCreateViewModel
    {
        [Required(ErrorMessage = "Enter Full Name")]
        [Display(Name = "Full Name")]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required(ErrorMessage = "Enter Username")]
        [Display(Name = "Username")]
        [MaxLength(50)]
        public string Username { get; set; }

        [Required(ErrorMessage = "Enter email address")]
        [EmailAddress(ErrorMessage = "Enter valid email address")]
        [Display(Name = "Email")]
        [MaxLength(100)]
        public string Email { get; set; }

        [StringLength(11, ErrorMessage = "Enter 11 digit phone no. e.g. 01686xxxxxx")]
        [Display(Name = "Phone Number")]
        public string PhoneNo { get; set; }

        [Required(ErrorMessage = "Select User Group")]
        [Display(Name = "User Group")]
        public int UserGroupId { get; set; }

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, ErrorMessage = "Must be between 6 and 100 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirm Password is required")]
        [StringLength(100, ErrorMessage = "Must be between 6 and 100 characters", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Confirm Password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }

        [Display(Name = "Account Status")]
        public bool IsActive { get; set; } = true;

        public List<LkpUserGroup> ?UserGroups { get; set; }
    }
}
