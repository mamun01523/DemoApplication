using DemoApplication.DemoDbContextClasses;
using DemoApplication.Helpers;
using DemoApplication.Models.UserAccount;
using DemoApplication.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoApplication.Controllers
{
    public class FeedbackController : Controller
    {
        private readonly DemoDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(
            DemoDbContext context,
            IWebHostEnvironment environment,
            ILogger<FeedbackController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        // GET: /Feedback/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: /Feedback/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeedbackViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32(SessionHelper.UserId);
                if (userId == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                string imagePath = null;

                // Handle image upload
                if (model.ImageFile != null && model.ImageFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "feedback");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ImageFile.CopyToAsync(fileStream);
                    }

                    imagePath = $"/uploads/feedback/{uniqueFileName}";
                }

                var feedback = new Feedback
                {
                    UserId = userId.Value,
                    Subject = model.Subject,
                    Message = model.Message,
                    ImagePath = imagePath,
                    CreatedDate = DateTime.Now,
                    IsRead = false
                };

                _context.Feedbacks.Add(feedback);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Feedback submitted by user ID: {userId}");
                TempData["SuccessMessage"] = "Thank you for your feedback! We'll review it soon.";
                return RedirectToAction("Index", "Home");
            }

            return View(model);
        }

        // GET: /Feedback/MyFeedback
        public async Task<IActionResult> MyFeedback()
        {
            var userId = HttpContext.Session.GetInt32(SessionHelper.UserId);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var feedbacks = await _context.Feedbacks
                .Include(f => f.User)  // Add this line to include User data
                .Where(f => f.UserId == userId)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            return View(feedbacks);
        }

        // Admin only actions
        private bool IsAdmin()
        {
            var userGroupId = HttpContext.Session.GetInt32(SessionHelper.UserGroupId);
            return userGroupId == 1; // Assuming 1 is Admin
        }

        // GET: /Feedback/AdminList (Admin only)
        public async Task<IActionResult> AdminList()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var feedbacks = await _context.Feedbacks
                .Include(f => f.User)
                .OrderByDescending(f => f.CreatedDate)
                .ToListAsync();

            return View(feedbacks);
        }

        // GET: /Feedback/AdminView/5 (Admin only)
        public async Task<IActionResult> AdminView(int? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FeedbackId == id);

            if (feedback == null)
            {
                return NotFound();
            }

            // Mark as read if not already
            if (!feedback.IsRead)
            {
                feedback.IsRead = true;
                _context.Update(feedback);
                await _context.SaveChangesAsync();
            }

            return View(feedback);
        }

        // POST: /Feedback/AddAdminNote/5 (Admin only)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAdminNote(int id, string adminNotes)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            feedback.AdminNotes = adminNotes;
            _context.Update(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Admin note added successfully.";
            return RedirectToAction("AdminView", new { id });
        }

        // GET: /Feedback/Delete/5 (Admin only)
        public async Task<IActionResult> Delete(int? id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (id == null)
            {
                return NotFound();
            }

            var feedback = await _context.Feedbacks
                .Include(f => f.User)
                .FirstOrDefaultAsync(f => f.FeedbackId == id);

            if (feedback == null)
            {
                return NotFound();
            }

            return View(feedback);
        }

        // POST: /Feedback/Delete/5 (Admin only)
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return NotFound();
            }

            // Delete image file if exists
            if (!string.IsNullOrEmpty(feedback.ImagePath))
            {
                var imagePath = Path.Combine(_environment.WebRootPath, feedback.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(imagePath))
                {
                    System.IO.File.Delete(imagePath);
                }
            }

            _context.Feedbacks.Remove(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Feedback deleted successfully.";
            return RedirectToAction("AdminList");
        }

        // POST: /Feedback/MarkAsRead (Admin only)
        [HttpPost]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            if (!IsAdmin())
            {
                return Json(new { success = false, message = "Access denied" });
            }

            var feedback = await _context.Feedbacks.FindAsync(id);
            if (feedback == null)
            {
                return Json(new { success = false, message = "Feedback not found" });
            }

            feedback.IsRead = true;
            _context.Update(feedback);
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }
    }
}
