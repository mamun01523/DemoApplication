using DemoApplication.DemoDbContextClasses;
using DemoApplication.Helpers;
using DemoApplication.Models.UserAccount;
using DemoApplication.Services;
using DemoApplication.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;


namespace DemoApplication.Controllers
{
    public class AccountController : Controller
    {
        private readonly DemoDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly ILogger<AccountController> _logger;


        public AccountController(DemoDbContext context, IConfiguration configuration, IEmailService emailService, ILogger<AccountController> logger)
        //public AccountController(DemoDbContext context, IConfiguration configuration, ILogger<AccountController> logger)
        {
            _context = context;
            _configuration = configuration;
            //new
            _emailService = emailService;
            _logger = logger;
        }

       
        // GET: /Account/Login
        public IActionResult Login()
        {
            if (HttpContext.Session.GetInt32(SessionHelper.UserId) != null)
                return RedirectToAction("Index", "Home");

            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users
                    .Include(u => u.UserGroup)
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.IsActive);

                if (user != null && PasswordHelper.VerifyPasswordHash(model.Password, user.PasswordHash, user.PasswordSalt))
                {
                    // Set session
                    HttpContext.Session.SetInt32(SessionHelper.UserId, user.UserId);
                    HttpContext.Session.SetString(SessionHelper.Username, user.Username);
                    HttpContext.Session.SetString(SessionHelper.FullName, user.FullName);
                    HttpContext.Session.SetInt32(SessionHelper.UserGroupId, user.UserGroupId);
                    HttpContext.Session.SetString(SessionHelper.UserGroupName, user.UserGroup.UserGroupName);
                    HttpContext.Session.SetString(SessionHelper.Email, user.Email);

                    // Set cookie for remember me
                    if (model.RememberMe)
                    {
                        var cookieOptions = new CookieOptions
                        {
                            Expires = DateTime.Now.AddDays(30),
                            HttpOnly = true
                        };
                        Response.Cookies.Append("RememberMe", user.UserId.ToString(), cookieOptions);
                    }

                    return RedirectToAction("Index", "Home");
                }

                ModelState.AddModelError("", "Invalid username or password.");
            }

            return View(model);
        }

        // GET: /Account/Register
        public async Task<IActionResult> Register()
        {
            var model = new RegistrationViewModel
            {
                UserGroups = await _context.UserGroups.ToListAsync()
            };
            return View(model);
        }

        // POST: /Account/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegistrationViewModel model)
        {
            //if (ModelState.IsValid)
            //{
                // Check if username already exists
                if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    model.UserGroups = await _context.UserGroups.ToListAsync();
                    return View(model);
                }

                // Check if email already exists
                if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    model.UserGroups = await _context.UserGroups.ToListAsync();
                    return View(model);
                }

                // Create password hash
                PasswordHelper.CreatePasswordHash(model.Password, out string passwordHash, out string passwordSalt);

                var user = new User
                {
                    FullName = model.FullName,
                    Username = model.Username,
                    Email = model.Email,
                    PhoneNo = model.PhoneNo,
                    UserGroupId = model.UserGroupId,
                    PasswordHash = passwordHash,
                    PasswordSalt = passwordSalt,
                    CreatedDate = DateTime.Now,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Registration successful! Please login.";
                return RedirectToAction("Login");
            //}

            model.UserGroups = await _context.UserGroups.ToListAsync();
            return View(model);
        }

        // GET: /Account/ForgotPassword
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Account/ForgotPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == model.Email && u.IsActive);

                if (user != null)
                {
                    // Generate reset token
                    var resetToken = PasswordHelper.GeneratePasswordResetToken();
                    user.ResetToken = resetToken;
                    user.ResetTokenExpiry = DateTime.Now.AddHours(1); // Token valid for 24 hours

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    try
                    {
                        // Send reset email

                        //var emailService = HttpContext.RequestServices.GetRequiredService<IEmailService>();
                        //await emailService.SendPasswordResetEmailAsync(user.Email, user.FullName, resetToken);

                        await _emailService.SendPasswordResetEmailAsync(user.Email, user.Username, resetToken);

                        TempData["SuccessMessage"] = "Password reset link has been sent to your email.";
                        return RedirectToAction("ForgotPassword");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to send reset email");
                        TempData["ErrorMessage"] = "Failed to send reset email. Please try again later.";
                    }
                }
                else
                {
                    // Don't reveal that the user doesn't exist for security
                    TempData["SuccessMessage"] = "If your email is registered, you will receive a password reset link.";
                    return RedirectToAction("ForgotPassword");
                }
            }

            return View(model);
        }
        // GET: /Account/ResetPassword
        public IActionResult ForgotChangePassword(string token = null, string email = null)
        {
            var model = new ForgotChangePasswordViewModel();

            if (!string.IsNullOrEmpty(token))
                model.Token = token;

            if (!string.IsNullOrEmpty(email))
                model.Email = email;

            // For demo: check if we have token from ForgotPassword
            if (TempData["ResetToken"] != null)
                model.Token = TempData["ResetToken"] as string;

            if (TempData["Email"] != null)
                model.Email = TempData["Email"] as string;

            return View(model);
        }

        // POST: /Account/ResetPassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotChangePassword(ForgotChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = await _context.Users.FirstOrDefaultAsync(u =>
                    u.Email == model.Email &&
                    u.ResetToken == model.Token &&
                    u.ResetTokenExpiry > DateTime.Now &&
                    u.IsActive);

                if (user != null)
                {
                    // Update password
                    PasswordHelper.CreatePasswordHash(model.NewPassword, out string passwordHash, out string passwordSalt);

                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;
                    user.ResetToken = null;
                    user.ResetTokenExpiry = null;

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Password reset successful! Please login with your new password.";
                    return RedirectToAction("Login");
                }

                ModelState.AddModelError("", "Invalid or expired reset token.");
            }

            return View(model);
        }

        // GET: /Account/ChangePassword
        public IActionResult ChangePassword()
        {
            var userId = HttpContext.Session.GetInt32(SessionHelper.UserId);
            if (userId == null)
                return RedirectToAction("Login");

            return View();
        }

        // POST: /Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32(SessionHelper.UserId);
                if (userId == null)
                    return RedirectToAction("Login");

                var user = await _context.Users.FindAsync(userId);

                if (user != null && PasswordHelper.VerifyPasswordHash(model.CurrentPassword, user.PasswordHash, user.PasswordSalt))
                {
                    // Update password
                    PasswordHelper.CreatePasswordHash(model.NewPassword, out string passwordHash, out string passwordSalt);

                    user.PasswordHash = passwordHash;
                    user.PasswordSalt = passwordSalt;

                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Password changed successfully!";
                    return RedirectToAction("Profile");
                }

                ModelState.AddModelError("CurrentPassword", "Current password is incorrect.");
            }

            return View(model);
        }

        // GET: /Account/Profile
        public async Task<IActionResult> Profile()
        {
            var userId = HttpContext.Session.GetInt32(SessionHelper.UserId);
            if (userId == null)
                return RedirectToAction("Login");

            var user = await _context.Users
                .Include(u => u.UserGroup)
                .FirstOrDefaultAsync(u => u.UserId == userId);

            if (user == null)
                return RedirectToAction("Login");

            return View(user);
        }

        // GET: /Account/Logout
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            Response.Cookies.Delete("RememberMe");
            return RedirectToAction("Login");
        }

        // GET: /Account/AccessDenied
        public IActionResult AccessDenied()
        {
            TempData["ErrorMessage"] = "You don't have permission to access this page.";
            return View();
        }


    }
}
