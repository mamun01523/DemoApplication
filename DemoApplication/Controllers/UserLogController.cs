using DemoApplication.DemoDbContextClasses;
using DemoApplication.Helpers;
using DemoApplication.Services;
using DemoApplication.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoApplication.Controllers
{
    public class UserLogController : Controller
    {
        private readonly DemoDbContext _context;
        private readonly IUserLogService _userLogService;
        private readonly ILogger<UserLogController> _logger;

        public UserLogController(DemoDbContext context,IUserLogService userLogService,ILogger<UserLogController> logger)
        {
            _context = context;
            _userLogService = userLogService;
            _logger = logger;
        }

        private bool IsAdmin()
        {
            var userGroupId = HttpContext.Session.GetInt32(SessionHelper.UserGroupId);
            return userGroupId == 1; // Assuming 1 is Admin
        }

        // GET: /UserLog/Index (Admin only)
        public async Task<IActionResult> UserLog(DateTime? startDate, DateTime? endDate, int? userId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            // Set default date range (last 7 days)
            if (!startDate.HasValue)
            {
                startDate = DateTime.Now.AddDays(-7);
            }
            if (!endDate.HasValue)
            {
                endDate = DateTime.Now;
            }

            var logs = await _userLogService.GetUserLogsAsync(userId, startDate, endDate);

            var viewModel = logs.Select(log => new UserLogViewModel
            {
                LogId = log.LogId,
                UserId = log.UserId,
                UserName = log.User?.Username,
                FullName = log.User?.FullName,
                IpAddress = log.IpAddress,
                LoginTime = log.LoginTime,
                LogoutTime = log.LogoutTime,
                SessionDurationMinutes = log.SessionDurationMinutes,
                UserAgent = log.UserAgent,
                Status = log.LogoutTime.HasValue ? "Logged Out" : "Active"
            }).ToList();

            ViewBag.StartDate = startDate.Value.ToString("yyyy-MM-dd");
            ViewBag.EndDate = endDate.Value.ToString("yyyy-MM-dd");
            ViewBag.SelectedUserId = userId;
            ViewBag.Users = await _context.Users.ToListAsync();

            // Calculate statistics
            ViewBag.TotalLogins = logs.Count;
            ViewBag.ActiveSessions = logs.Count(l => !l.LogoutTime.HasValue);
            ViewBag.AverageSessionMinutes = logs
                .Where(l => l.SessionDurationMinutes.HasValue)
                .Average(l => l.SessionDurationMinutes) ?? 0;

            return View(viewModel);
        }

        // GET: /UserLog/Export (Admin only)
        public async Task<IActionResult> Export(DateTime? startDate, DateTime? endDate, int? userId)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var logs = await _userLogService.GetUserLogsAsync(userId, startDate, endDate);

            var csvData = new List<string>
            {
                "Log ID,User ID,Username,Full Name,IP Address,Login Time,Logout Time,Session Duration (minutes),User Agent,Status"
            };

            foreach (var log in logs)
            {
                csvData.Add($"\"{log.LogId}\",\"{log.UserId}\",\"{log.User?.Username}\",\"{log.User?.FullName}\",\"{log.IpAddress}\",\"{log.LoginTime}\",\"{(log.LogoutTime.HasValue ? log.LogoutTime.Value.ToString() : "N/A")}\",\"{log.SessionDurationMinutes}\",\"{log.UserAgent}\",\"{(log.LogoutTime.HasValue ? "Logged Out" : "Active")}\"");
            }

            var csvContent = string.Join("\n", csvData);
            var fileName = $"user_logs_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            return File(System.Text.Encoding.UTF8.GetBytes(csvContent), "text/csv", fileName);
        }

        // GET: /UserLog/UserActivity/5 (Admin only)
        public async Task<IActionResult> UserActivity(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var logs = await _userLogService.GetUserLogsAsync(id);

            ViewBag.User = user;

            // Calculate user statistics
            ViewBag.TotalLogins = logs.Count;
            ViewBag.ActiveSessions = logs.Count(l => !l.LogoutTime.HasValue);
            ViewBag.AverageSessionMinutes = logs
                .Where(l => l.SessionDurationMinutes.HasValue)
                .Average(l => l.SessionDurationMinutes) ?? 0;
            ViewBag.LastLogin = logs.FirstOrDefault()?.LoginTime;
            ViewBag.TotalSessionMinutes = logs
                .Where(l => l.SessionDurationMinutes.HasValue)
                .Sum(l => l.SessionDurationMinutes) ?? 0;

            return View(logs);
        }

        // GET: /UserLog/ClearOldLogs (Admin only)
        public async Task<IActionResult> ClearOldLogs(int daysToKeep = 30)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
            var oldLogs = await _context.UserLogs
                .Where(l => l.LoginTime < cutoffDate)
                .ToListAsync();

            if (!oldLogs.Any())
            {
                TempData["InfoMessage"] = $"No log entries older than {daysToKeep} days were found.";
                return RedirectToAction("UserLog");
            }

            _context.UserLogs.RemoveRange(oldLogs);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cleared {oldLogs.Count} log entries older than {daysToKeep} days.";
            return RedirectToAction("UserLog");
        }
    }
}
