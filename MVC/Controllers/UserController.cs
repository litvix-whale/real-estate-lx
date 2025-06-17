using Microsoft.AspNetCore.Mvc;
using Core.Interfaces;
using MVC.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace MVC.Controllers
{
    public class UserController(IUserService userService) : Controller
    {
        private readonly IUserService _userService = userService;

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userService.RegisterUserAsync(model.Email, model.UserName, model.Password, model.RememberMe);

                if (result == "Success")
                {
                    return RedirectToAction("Index", "Post");
                }

                ModelState.AddModelError(string.Empty, result);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                var result = await _userService.LoginUserAsync(model.Email, model.Password, model.RememberMe);

                if (result == "Success")
                {
                    return RedirectToAction("Index", "Post");
                }

                ModelState.AddModelError(string.Empty, result);
            }

            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await _userService.LogoutUserAsync();
            return RedirectToAction("Index", "Post");
        }

        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Users(string sortOrder = "", string searchTerm = "", string filter = "all", int page = 1, int pageSize = 10)
        {
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var allUsers = await _userService.GetUsersByRoleAsync("Default");
            var filteredUsers = allUsers.AsQueryable();

            if (!string.IsNullOrEmpty(searchTerm))
            {
                filteredUsers = filteredUsers.Where(u =>
                    u.UserName != null && u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Email != null && u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
            }

            switch (filter.ToLower())
            {
                case "active":
                    filteredUsers = filteredUsers.Where(u => !u.BannedTo.HasValue || u.BannedTo < DateTime.UtcNow);
                    break;
                case "banned":
                    filteredUsers = filteredUsers.Where(u => u.BannedTo.HasValue && u.BannedTo > DateTime.UtcNow);
                    break;
                default: // "all"
                    break;
            }

            filteredUsers = sortOrder switch
            {
                "username_asc" => filteredUsers.OrderBy(u => u.UserName),
                "username_desc" => filteredUsers.OrderByDescending(u => u.UserName),
                "date_asc" => filteredUsers.OrderBy(u => u.CreatedAt),
                "date_desc" => filteredUsers.OrderByDescending(u => u.CreatedAt),
                _ => filteredUsers.OrderBy(u => u.UserName) // Default sort
            };

            int totalItems = filteredUsers.Count();
            int totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            page = Math.Max(1, Math.Min(page, Math.Max(1, totalPages)));

            var pagedUsers = filteredUsers
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var model = new UsersViewModel
            {
                Users = pagedUsers,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalUsers = totalItems,
                CurrentFilter = searchTerm,
                StatusFilter = filter,
                SortOrder = sortOrder
            };

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string email)
        {
            var result = await _userService.DeleteUserAsync(email);

            if (result == "Success")
            {
                return RedirectToAction("Users", "User");
            }

            ModelState.AddModelError(string.Empty, result);
            return RedirectToAction("Users", "User");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> BanUser(string email, DateTime? bannedTo)
        {
            var result = await _userService.BanUserAsync(email, bannedTo);

            if (result == "Success")
            {
                return RedirectToAction("Users", "User");
            }

            ModelState.AddModelError(string.Empty, result);
            return RedirectToAction("Users", "User");
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UnbanUserAsync(string email)
        {
            var result = await _userService.UnbanUserAsync(email);

            if (result == "Success")
            {
                return RedirectToAction("Users", "User");
            }

            ModelState.AddModelError(string.Empty, result);
            return RedirectToAction("Users", "User");
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            var user = await _userService.GetUserByUserNameAsync(User.Identity?.Name!);
            
            var model = new ProfileViewModel 
            { 
                Email = user.Email, 
                UserName = user.UserName, 
                ProfilePicture = user.ProfilePicture,
                NewUserName = user.UserName,
            };
            
            return View(model);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateProfile(ProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View("Profile", model);
            }

            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
            {
                ModelState.AddModelError(string.Empty, "User not authenticated");
                return View("Profile", model);
            }

            var user = await _userService.GetUserByUserNameAsync(username);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "User not found");
                return View("Profile", model);
            }
            
            // Update user information
            var result = await _userService.UpdateUserProfileAsync(user, model.NewUserName!, model.NewProfilePicture!);

            if (result == "Success")
            {
                // Refresh model with updated data
                user = await _userService.GetUserByUserNameAsync(username);
                model.UserName = user.UserName;
                model.ProfilePicture = user.ProfilePicture;
                
                TempData["SuccessMessage"] = "Profile updated successfully!";
            }
            else if (!string.IsNullOrEmpty(result)) // Only add error if result is not null/empty
            {
                ModelState.AddModelError(string.Empty, result);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Failed to update profile");
            }
            
            return View("Profile", model);
        }
    }
}
