using Microsoft.AspNetCore.Mvc;
using WashWhiz.Data;
using WashWhiz.Models;

namespace WashWhiz.Controllers
{
    [Route("Laundry")]
    public class OrdersController : Controller
    {
        private readonly WashWhizContext _context;

        public OrdersController(WashWhizContext context)
        {
            _context = context;
        }

        private bool IsLoggedIn()
        {
            // Use UserEmail in session to determine logged-in user
            return !string.IsNullOrEmpty(HttpContext.Session.GetString("UserEmail")) ||
                   HttpContext.Session.GetString("UserRole") == "Admin";
        }

        private bool IsAdmin()
        {
            return HttpContext.Session.GetString("UserRole") == "Admin";
        }

        [HttpGet]
        [Route("Dashboard")]
        public IActionResult Index(string? searchString, string? statusFilter)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = _context.Orders.AsQueryable();

            // Exclude Completed from the dashboard for both Admin and Users
            if (IsAdmin())
            {
                orders = orders.Where(o => o.Status != "Completed");
            }
            else
            {
                var userEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
                orders = orders.Where(o => o.UserEmail == userEmail && o.Status != "Completed");
            }

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                orders = orders.Where(o => o.CustomerName.Contains(searchString));
            }

            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                orders = orders.Where(o => o.Status == statusFilter);
            }

            ViewBag.IsAdmin = IsAdmin();

            // Order by OrderDate
            return View(orders.OrderByDescending(o => o.OrderDate).ToList());
        }

        [HttpGet]
        [Route("NewOrder")]
        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            // Provide flags and session email for the view
            ViewBag.IsAdmin = IsAdmin();
            ViewBag.SessionEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("NewOrder")]
        public IActionResult Create(LaundryOrder order)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var sessionEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            var isAdmin = IsAdmin();

            if (ModelState.IsValid)
            {
                // If the creator is NOT an admin, force their session email.
                if (!isAdmin)
                {
                    order.UserEmail = sessionEmail;
                }
                else
                {
                    // Admin-provided UserEmail should remain as entered; ensure non-null
                    order.UserEmail = order.UserEmail ?? string.Empty;
                }

                order.OrderDate = DateTime.Now;
                order.Status = "Pending";

                _context.Orders.Add(order);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Order submitted successfully!";
                return RedirectToAction("Index");
            }

            // Re-populate view flags when returning view with validation errors
            ViewBag.IsAdmin = isAdmin;
            ViewBag.SessionEmail = sessionEmail;

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("UpdateStatus")]
        public IActionResult UpdateStatus(int id, string status)
        {
            if (!IsAdmin())
            {
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders.FirstOrDefault(o => o.LaundryOrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            order.Status = status;
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Order status updated successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("CancelOrder")]
        public IActionResult CancelOrder(int id)
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var order = _context.Orders.FirstOrDefault(o => o.LaundryOrderId == id);

            if (order == null)
            {
                return NotFound();
            }

            if (!IsAdmin())
            {
                var userEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;

                if (order.UserEmail != userEmail)
                {
                    return RedirectToAction("Index");
                }
            }

            _context.Orders.Remove(order);
            _context.SaveChanges();

            TempData["SuccessMessage"] = IsAdmin()
                   ? "Order deleted successfully!"
                   : "Order cancelled successfully!";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("CompleteOrder")]
        [Route("Orders/CompleteOrder")] // allow conventional URL too
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CompleteOrder(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            order.Status = "Completed";
            _context.Update(order);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order marked as completed.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [Route("History")]
        public IActionResult History()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = _context.Orders.AsQueryable();

            if (IsAdmin())
            {
                // Admin sees all completed orders
                orders = orders.Where(o => o.Status == "Completed");
            }
            else
            {
                var userEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
                // User sees only their own completed orders
                orders = orders.Where(o => o.UserEmail == userEmail && o.Status == "Completed");
            }

            ViewBag.IsAdmin = IsAdmin();

            return View(orders.OrderByDescending(o => o.OrderDate).ToList());
        }
    }
}