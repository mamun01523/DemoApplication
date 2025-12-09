using DemoApplication.DemoDbContextClasses;
using DemoApplication.Helpers;
using DemoApplication.Models.UserAccount;
using DemoApplication.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DemoApplication.Controllers
{
    //[Authorize]
    public class UserManagementController : Controller
    {
        private readonly DemoDbContext _context;
        private readonly ILogger<UserManagementController> _logger;

        public UserManagementController(DemoDbContext context, ILogger<UserManagementController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Check if user is admin
        private bool IsAdmin()
        {
            var userGroupId = HttpContext.Session.GetInt32(SessionHelper.UserGroupId);
            return userGroupId == 1; // Assuming 1 is Admin
        }

        //User Management Actions

        // GET: /UserManagement/Users
        public async Task<IActionResult> Users()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var users = await _context.Users
                .Include(u => u.UserGroup)
                .OrderBy(u => u.FullName)
                .ToListAsync();

            return View(users);
        }

        // GET: /UserManagement/CreateUser
        public async Task<IActionResult> CreateUser()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var model = new UserCreateViewModel
            {
                UserGroups = await _context.UserGroups.ToListAsync()
            };

            return View(model);
        }

        // POST: /UserManagement/CreateUser
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(UserCreateViewModel model)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
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
                    IsActive = model.IsActive
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User created: {user.Username} (ID: {user.UserId}) by admin");
                TempData["SuccessMessage"] = $"User '{user.FullName}' created successfully!";
                return RedirectToAction("Users");
            }

            model.UserGroups = await _context.UserGroups.ToListAsync();
            return View(model);
        }

        // GET: /UserManagement/EditUser/5
        public async Task<IActionResult> EditUser(int? id)
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

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new UserEditViewModel
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Username = user.Username,
                Email = user.Email,
                PhoneNo = user.PhoneNo,
                UserGroupId = user.UserGroupId,
                IsActive = user.IsActive,
                UserGroups = await _context.UserGroups.ToListAsync()
            };

            return View(model);
        }

        // POST: /UserManagement/EditUser/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(int id, UserEditViewModel model)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (id != model.UserId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Check if username is changed and already exists
                if (user.Username != model.Username &&
                    await _context.Users.AnyAsync(u => u.Username == model.Username && u.UserId != id))
                {
                    ModelState.AddModelError("Username", "Username already exists.");
                    model.UserGroups = await _context.UserGroups.ToListAsync();
                    return View(model);
                }

                // Check if email is changed and already exists
                if (user.Email != model.Email &&
                    await _context.Users.AnyAsync(u => u.Email == model.Email && u.UserId != id))
                {
                    ModelState.AddModelError("Email", "Email already registered.");
                    model.UserGroups = await _context.UserGroups.ToListAsync();
                    return View(model);
                }

                // Update user properties
                user.FullName = model.FullName;
                user.Username = model.Username;
                user.Email = model.Email;
                user.PhoneNo = model.PhoneNo;
                user.UserGroupId = model.UserGroupId;
                user.IsActive = model.IsActive;

                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User updated: {user.Username} (ID: {user.UserId})");
                    TempData["SuccessMessage"] = $"User '{user.FullName}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Users");
            }

            model.UserGroups = await _context.UserGroups.ToListAsync();
            return View(model);
        }

        // GET: /UserManagement/DeleteUser/5
        public async Task<IActionResult> DeleteUser(int? id)
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

            var user = await _context.Users
                .Include(u => u.UserGroup)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            // Prevent deleting the last admin
            if (user.UserGroupId == 1) // Admin
            {
                var adminCount = await _context.Users.CountAsync(u => u.UserGroupId == 1 && u.IsActive);
                if (adminCount <= 1)
                {
                    TempData["ErrorMessage"] = "Cannot delete the last active admin user.";
                    return RedirectToAction("Users");
                }
            }

            // Prevent self-deletion
            var currentUserId = HttpContext.Session.GetInt32(SessionHelper.UserId);
            if (user.UserId == currentUserId)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToAction("Users");
            }

            return View(user);
        }

        // POST: /UserManagement/DeleteUser/5
        [HttpPost, ActionName("DeleteUser")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserConfirmed(int id)
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

            var userFullName = user.FullName;

            // Soft delete (deactivate) instead of hard delete
            user.IsActive = false;
            _context.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User deactivated: {user.Username} (ID: {user.UserId})");
            TempData["SuccessMessage"] = $"User '{userFullName}' has been deactivated successfully!";

            return RedirectToAction("Users");
        }

        // GET: /UserManagement/ActivateUser/5
        public async Task<IActionResult> ActivateUser(int id)
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

            user.IsActive = true;
            _context.Update(user);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"User '{user.FullName}' has been activated successfully!";
            return RedirectToAction("Users");
        }

        // GET: /UserManagement/ResetUserPassword/5
        public async Task<IActionResult> ResetUserPassword(int? id)
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

            var user = await _context.Users
                .Include(u => u.UserGroup)
                .FirstOrDefaultAsync(u => u.UserId == id);

            if (user == null)
            {
                return NotFound();
            }

            ViewBag.UserName = user.Username;
            ViewBag.FullName = user.FullName;

            return View();
        }

        // POST: /UserManagement/ResetUserPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUserPassword(int id, string NewPassword, string ConfirmPassword)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (string.IsNullOrEmpty(NewPassword) || string.IsNullOrEmpty(ConfirmPassword))
            {
                TempData["ErrorMessage"] = "Please enter both password fields.";
                return RedirectToAction("ResetUserPassword", new { id });
            }

            if (NewPassword != ConfirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match.";
                return RedirectToAction("ResetUserPassword", new { id });
            }

            if (NewPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Password must be at least 6 characters long.";
                return RedirectToAction("ResetUserPassword", new { id });
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            // Create new password hash
            PasswordHelper.CreatePasswordHash(NewPassword, out string passwordHash, out string passwordSalt);

            user.PasswordHash = passwordHash;
            user.PasswordSalt = passwordSalt;
            user.ResetToken = null;
            user.ResetTokenExpiry = null;

            _context.Update(user);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Password reset for user: {user.Username} (ID: {user.UserId}) by admin");
            TempData["SuccessMessage"] = $"Password for '{user.FullName}' has been reset successfully!";

            return RedirectToAction("Users");
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserId == id);
        }

        

        // User Group Management Actions

        // GET: /UserManagement/UserGroups
        public async Task<IActionResult> UserGroups()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var userGroups = await _context.UserGroups
                .Include(ug => ug.Users)
                .OrderBy(ug => ug.UserGroupName)
                .ToListAsync();

            return View(userGroups);
        }

        // GET: /UserManagement/CreateUserGroup
        public IActionResult CreateUserGroup()
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: /UserManagement/CreateUserGroup
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUserGroup(UserGroupViewModel model)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (ModelState.IsValid)
            {
                // Check if user group name already exists
                if (await _context.UserGroups.AnyAsync(ug => ug.UserGroupName == model.UserGroupName))
                {
                    ModelState.AddModelError("UserGroupName", "User Group Name already exists.");
                    return View(model);
                }

                var userGroup = new LkpUserGroup
                {
                    UserGroupName = model.UserGroupName
                };

                _context.UserGroups.Add(userGroup);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User Group created: {userGroup.UserGroupName} (ID: {userGroup.UserGroupId})");
                TempData["SuccessMessage"] = $"User Group '{userGroup.UserGroupName}' created successfully!";
                return RedirectToAction("UserGroups");
            }

            return View(model);
        }

        // GET: /UserManagement/EditUserGroup/5
        public async Task<IActionResult> EditUserGroup(int? id)
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

            var userGroup = await _context.UserGroups.FindAsync(id);
            if (userGroup == null)
            {
                return NotFound();
            }

            var model = new UserGroupViewModel
            {
                UserGroupId = userGroup.UserGroupId,
                UserGroupName = userGroup.UserGroupName
            };

            return View(model);
        }

        // POST: /UserManagement/EditUserGroup/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUserGroup(int id, UserGroupViewModel model)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            if (id != model.UserGroupId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var userGroup = await _context.UserGroups.FindAsync(id);
                if (userGroup == null)
                {
                    return NotFound();
                }

                // Check if user group name is changed and already exists
                if (userGroup.UserGroupName != model.UserGroupName &&
                    await _context.UserGroups.AnyAsync(ug => ug.UserGroupName == model.UserGroupName))
                {
                    ModelState.AddModelError("UserGroupName", "User Group Name already exists.");
                    return View(model);
                }

                // Prevent editing default admin and user groups
                if (id <= 2) // Assuming 1=Admin, 2=User are default groups
                {
                    TempData["ErrorMessage"] = "Cannot edit default user groups.";
                    return RedirectToAction("UserGroups");
                }

                userGroup.UserGroupName = model.UserGroupName;

                try
                {
                    _context.Update(userGroup);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation($"User Group updated: {userGroup.UserGroupName} (ID: {userGroup.UserGroupId})");
                    TempData["SuccessMessage"] = $"User Group '{userGroup.UserGroupName}' updated successfully!";
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserGroupExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("UserGroups");
            }

            return View(model);
        }

        // GET: /UserManagement/DeleteUserGroup/5
        public async Task<IActionResult> DeleteUserGroup(int? id)
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

            var userGroup = await _context.UserGroups
                .Include(ug => ug.Users)
                .FirstOrDefaultAsync(ug => ug.UserGroupId == id);

            if (userGroup == null)
            {
                return NotFound();
            }

            // Check if there are users in this group
            if (userGroup.Users != null && userGroup.Users.Any())
            {
                TempData["ErrorMessage"] = $"Cannot delete user group '{userGroup.UserGroupName}' because it has users assigned. Reassign users first.";
                return RedirectToAction("UserGroups");
            }

            // Prevent deleting default groups
            if (id <= 2) // Assuming 1=Admin, 2=User are default groups
            {
                TempData["ErrorMessage"] = "Cannot delete default user groups.";
                return RedirectToAction("UserGroups");
            }

            return View(userGroup);
        }

        // POST: /UserManagement/DeleteUserGroup/5
        [HttpPost, ActionName("DeleteUserGroup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUserGroupConfirmed(int id)
        {
            if (!IsAdmin())
            {
                TempData["ErrorMessage"] = "Access denied. Admin privileges required.";
                return RedirectToAction("Index", "Home");
            }

            var userGroup = await _context.UserGroups.FindAsync(id);
            if (userGroup == null)
            {
                return NotFound();
            }

            // Double-check for users in this group
            var hasUsers = await _context.Users.AnyAsync(u => u.UserGroupId == id);
            if (hasUsers)
            {
                TempData["ErrorMessage"] = $"Cannot delete user group '{userGroup.UserGroupName}' because it has users assigned. Reassign users first.";
                return RedirectToAction("UserGroups");
            }

            var groupName = userGroup.UserGroupName;

            _context.UserGroups.Remove(userGroup);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"User Group deleted: {groupName} (ID: {id})");
            TempData["SuccessMessage"] = $"User Group '{groupName}' deleted successfully!";

            return RedirectToAction("UserGroups");
        }

        private bool UserGroupExists(int id)
        {
            return _context.UserGroups.Any(e => e.UserGroupId == id);
        }

       

    }
}
