namespace DemoApplication.ViewModels
{
    public class UserLogViewModel
    {
        public int LogId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string IpAddress { get; set; }
        public DateTime LoginTime { get; set; }
        public DateTime? LogoutTime { get; set; }
        public int? SessionDurationMinutes { get; set; }
        public string UserAgent { get; set; }
        public string Status { get; set; }
    }
}
