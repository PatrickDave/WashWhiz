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
        public IActionResult Login(string role, string email, string username, string password)
        {
            // ===================== ADMIN LOGIN LOGIC =====================
            if (role == "Admin")
            {
                UserAccount adminUser = null;

                foreach (var u in _context.Users)
                {
                    if (u.Username == username && u.Password == password && u.Role == "Admin")
                    {
                        adminUser = u;
                        break;
                    }
                }

                if (adminUser != null)
                {
                    SetUserSession(adminUser);
                    return RedirectToAction("Index", "Orders");
                }
            }
            // ===================== USER LOGIN LOGIC =====================
            else
            {
                UserAccount user = null;
 
                foreach (var u in _context.Users)
                {
                    if (u.Email == email && u.Password == password)
                    {
                        user = u;
                        break;
                    }
                }

                if (user != null)
                {
                    SetUserSession(user);

                    // if a users is an admin, redirect to admin dashboard, otherwise redirect to user dashboard
                    if (user.Role == "Admin")
                    {
                        return RedirectToAction("Index", "Orders");
                    }

                    return RedirectToAction("Dashboard", "Laundry");
                }
            }

            // if you choose admin as admin in login the label for email will be username, otherwise it will be email.
            string identityLabel;
            if (role == "Admin")
            {
                identityLabel = "username";
            }
            else
            {
                identityLabel = "email";
            }

            ViewBag.Error = "Invalid credentials. Please check your " + identityLabel + " and password.";
            return View();
        }

        // stores a session for the logged in user so that other controllers can access the user's information and use it for ownership and authorization
        private void SetUserSession(UserAccount user)
        {
            HttpContext.Session.SetInt32("UserId", user.UserId);
            HttpContext.Session.SetString("UserName", user.FullName);
            HttpContext.Session.SetString("UserRole", user.Role ?? "User");
            HttpContext.Session.SetString("ProfilePicture", user.ProfilePicture ?? "default.png");

            // Store the user's email so controllers can use UserEmail for ownership
            HttpContext.Session.SetString("UserEmail", user.Email ?? string.Empty);
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
        public IActionResult Register(UserAccount user, IFormFile profilePic)
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
                        profilePic.CopyTo(stream);
                    }

                    user.ProfilePicture = fileName;
                }
                else
                {
                    user.ProfilePicture = "default.png";
                }

                user.Role = "User";

                _context.Users.Add(user);
                _context.SaveChanges();

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
        public IActionResult Profile(
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
                    profilePic.CopyTo(stream);
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

            _context.SaveChanges();

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