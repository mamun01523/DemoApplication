using System.ComponentModel.DataAnnotations;

namespace DemoApplication.ViewModels
{
    public class FeedbackViewModel
    {
        [Required(ErrorMessage = "Subject is required")]
        [Display(Name = "Subject")]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required(ErrorMessage = "Message is required")]
        [Display(Name = "Message")]
        [MaxLength(2000)]
        public string Message { get; set; }

        [Display(Name = "Attachment")]
        public IFormFile ImageFile { get; set; }
    }
}
