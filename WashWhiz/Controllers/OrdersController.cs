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
            return HttpContext.Session.GetInt32("UserId") != null ||
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

            if (!IsAdmin())
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                orders = orders.Where(o => o.UserId == userId);
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

            return View(orders.OrderByDescending(o => o.DateCreated).ToList());
        }

        [HttpGet]
        [Route("NewOrder")]
        public IActionResult Create()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

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

            if (ModelState.IsValid)
            {
                var userId = HttpContext.Session.GetInt32("UserId");

                order.UserId = userId ?? 0;
                order.DateCreated = DateTime.Now;
                order.Status = "Pending";

                _context.Orders.Add(order);
                _context.SaveChanges();

                TempData["SuccessMessage"] = "Order submitted successfully!";
                return RedirectToAction("Index");
            }

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
                var userId = HttpContext.Session.GetInt32("UserId");

                if (order.UserId != userId)
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

        [HttpGet]
        [Route("History")]
        public IActionResult History()
        {
            if (!IsLoggedIn())
            {
                return RedirectToAction("Login", "Account");
            }

            var orders = _context.Orders.AsQueryable();

            if (!IsAdmin())
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                orders = orders.Where(o => o.UserId == userId);
            }

            ViewBag.IsAdmin = IsAdmin();

            return View(orders
                .Where(o => o.Status == "Ready")
                .OrderByDescending(o => o.DateCreated)
                .ToList());
        }
    }
}