using DemoApplication.Models.UserAccount;

namespace DemoApplication.Services
{
    public interface IUserLogService
    {
        Task LogLoginAsync(int userId, HttpContext httpContext);
        Task LogLogoutAsync(int userId);
        Task<List<UserLog>> GetUserLogsAsync(int? userId = null, DateTime? startDate = null, DateTime? endDate = null);
    }
}
