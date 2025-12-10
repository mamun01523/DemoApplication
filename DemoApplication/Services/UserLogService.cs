using DemoApplication.DemoDbContextClasses;
using DemoApplication.Models.UserAccount;
using Microsoft.EntityFrameworkCore;

namespace DemoApplication.Services
{
    public class UserLogService : IUserLogService
    {
        private readonly DemoDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public UserLogService(DemoDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task LogLoginAsync(int userId, HttpContext httpContext)
        {
            var ipAddress = GetIpAddress(httpContext);
            var userAgent = httpContext.Request.Headers["User-Agent"].ToString();

            var userLog = new UserLog
            {
                UserId = userId,
                IpAddress = ipAddress,
                LoginTime = DateTime.Now,
                UserAgent = userAgent
            };

            _context.UserLogs.Add(userLog);
            await _context.SaveChangesAsync();

            // Store log ID in session for logout tracking
            httpContext.Session.SetInt32("UserLogId", userLog.LogId);
        }

        public async Task LogLogoutAsync(int userId)
        {
            var httpContext = _httpContextAccessor.HttpContext;
            var logId = httpContext?.Session.GetInt32("UserLogId");

            if (logId.HasValue)
            {
                var userLog = await _context.UserLogs.FindAsync(logId.Value);
                if (userLog != null && userLog.UserId == userId && !userLog.LogoutTime.HasValue)
                {
                    userLog.LogoutTime = DateTime.Now;
                    userLog.SessionDurationMinutes = (int)(userLog.LogoutTime.Value - userLog.LoginTime).TotalMinutes;

                    _context.UserLogs.Update(userLog);
                    await _context.SaveChangesAsync();
                }
            }
        }

        public async Task<List<UserLog>> GetUserLogsAsync(int? userId = null, DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _context.UserLogs
                .Include(ul => ul.User)
                .OrderByDescending(ul => ul.LoginTime)
                .AsQueryable();

            if (userId.HasValue)
            {
                query = query.Where(ul => ul.UserId == userId.Value);
            }

            if (startDate.HasValue)
            {
                query = query.Where(ul => ul.LoginTime >= startDate.Value);
            }

            if (endDate.HasValue)
            {
                query = query.Where(ul => ul.LoginTime <= endDate.Value);
            }

            return await query.ToListAsync();
        }

        private string GetIpAddress(HttpContext httpContext)
        {
            // Try to get the IP from various headers
            var ipAddress = httpContext.Connection.RemoteIpAddress?.ToString();

            // Check for forwarded headers (behind proxy/load balancer)
            var forwardedHeader = httpContext.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedHeader))
            {
                ipAddress = forwardedHeader.Split(',')[0].Trim();
            }

            return ipAddress ?? "Unknown";
        }
    }
}
