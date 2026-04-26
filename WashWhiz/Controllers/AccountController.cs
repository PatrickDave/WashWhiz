using Microsoft.AspNetCore.Mvc;
using WashWhiz.Data;
using WashWhiz.Models;

namespace WashWhiz.Controllers
{
    [Route("Account")]
    public class AccountController : Controller
    {
        private readonly WashWhizContext _context;
        private readonly IWebHostEnvironment _environment;

        public AccountController(WashWhizContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // ===================== LOGIN =====================

        [HttpGet]
        [Route("Login")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Login")]
        public IActionResult Login(string email, string password)
        {
            // Hardcoded admin login
            if (email == "admin@washwhiz.com" && password == "admin123")
            {
                HttpContext.Session.SetInt32("UserId", 0);
                HttpContext.Session.SetString("UserName", "Administrator");
                HttpContext.Session.SetString("UserRole", "Admin");
                HttpContext.Session.SetString("ProfilePicture", "default.png");

                return RedirectToAction("Index", "Orders");
            }

            // Normal user login
            var user = _context.Users
                .FirstOrDefault(u => u.Email == email && u.Password == password);

            if (user != null)
            {
                HttpContext.Session.SetInt32("UserId", user.UserId);
                HttpContext.Session.SetString("UserName", user.FullName);
                HttpContext.Session.SetString("UserRole", user.Role);
                HttpContext.Session.SetString("ProfilePicture", user.ProfilePicture ?? "default.png");

                if (user.Role == "Admin")
                {
                    return RedirectToAction("Index", "Orders");
                }

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Error = "Invalid email or password.";
            return View();
        }

        // ===================== REGISTER =====================

        [HttpGet]
        [Route("Register")]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Register")]
        public async Task<IActionResult> Register(UserAccount user, IFormFile profilePic)
        {
            if (ModelState.IsValid)
            {
                if (profilePic != null && profilePic.Length > 0)
                {
                    string folder = Path.Combine(_environment.WebRootPath, "profile_pics");

                    if (!Directory.Exists(folder))
                    {
                        Directory.CreateDirectory(folder);
                    }

                    string fileName = Guid.NewGuid().ToString() + "_" + profilePic.FileName;
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await profilePic.CopyToAsync(stream);
                    }

                    user.ProfilePicture = fileName;
                }
                else
                {
                    user.ProfilePicture = "default.png";
                }

                user.Role = "User";

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Account created successfully! Please log in.";
                return RedirectToAction("Login");
            }

            return View(user);
        }

        // ===================== PROFILE =====================

        [HttpGet]
        [Route("Profile")]
        public IActionResult Profile()
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Profile")]
        public async Task<IActionResult> Profile(
    UserAccount updatedUser,
    IFormFile profilePic,
    string currentPassword,
    string newPassword,
    string confirmPassword)
        {
            var userId = HttpContext.Session.GetInt32("UserId");

            if (userId == null)
            {
                return RedirectToAction("Login");
            }

            var user = _context.Users.FirstOrDefault(u => u.UserId == userId);

            if (user == null)
            {
                return RedirectToAction("Login");
            }

            user.FullName = updatedUser.FullName;
            user.Email = updatedUser.Email;
            user.PhoneNumber = updatedUser.PhoneNumber;

            if (profilePic != null && profilePic.Length > 0)
            {
                string folder = Path.Combine(_environment.WebRootPath, "profile_pics");

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                string fileName = Guid.NewGuid().ToString() + "_" + profilePic.FileName;
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await profilePic.CopyToAsync(stream);
                }

                user.ProfilePicture = fileName;
            }

            if (!string.IsNullOrWhiteSpace(currentPassword) ||
                !string.IsNullOrWhiteSpace(newPassword) ||
                !string.IsNullOrWhiteSpace(confirmPassword))
            {
                if (currentPassword != user.Password)
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction("Profile");
                }

                if (newPassword != confirmPassword)
                {
                    TempData["ErrorMessage"] = "New password and confirmation password do not match.";
                    return RedirectToAction("Profile");
                }

                if (newPassword.Length < 6)
                {
                    TempData["ErrorMessage"] = "New password must be at least 6 characters.";
                    return RedirectToAction("Profile");
                }

                user.Password = newPassword;
            }

            await _context.SaveChangesAsync();

            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("ProfilePicture", user.ProfilePicture ?? "default.png");

            TempData["SuccessMessage"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }


        // ===================== LOGOUT =====================

        [HttpGet]
        [Route("Logout")]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }
}