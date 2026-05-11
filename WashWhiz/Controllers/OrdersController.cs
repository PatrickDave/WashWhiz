using System;
using System.Collections.Generic;
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

            var result = new List<LaundryOrder>();
            var isAdmin = IsAdmin();
            var sessionEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;

            // Collect orders according to admin/user rules (no LINQ/lambda)
            foreach (var o in _context.Orders)
            {
                if (isAdmin)
                {
                    if (o.Status != "Completed")
                    {
                        result.Add(o);
                    }
                }
                else
                {
                    if (o.UserEmail == sessionEmail && o.Status != "Completed")
                    {
                        result.Add(o);
                    }
                }
            }

            // Apply searchString filter (if provided)
            if (!string.IsNullOrWhiteSpace(searchString))
            {
                var filtered = new List<LaundryOrder>();
                foreach (var o in result)
                {
                    if (!string.IsNullOrEmpty(o.CustomerName) && o.CustomerName.Contains(searchString))
                    {
                        filtered.Add(o);
                    }
                }
                result = filtered;
            }

            // Apply statusFilter (if provided)
            if (!string.IsNullOrWhiteSpace(statusFilter))
            {
                var filtered = new List<LaundryOrder>();
                foreach (var o in result)
                {
                    if (o.Status == statusFilter)
                    {
                        filtered.Add(o);
                    }
                }
                result = filtered;
            }

            ViewBag.IsAdmin = isAdmin;

            // Sort by OrderDate descending using simple selection sort (explicit loops)
            for (int i = 0; i < result.Count - 1; i++)
            {
                int maxIndex = i;
                for (int j = i + 1; j < result.Count; j++)
                {
                    if (result[j].OrderDate > result[maxIndex].OrderDate)
                    {
                        maxIndex = j;
                    }
                }
                if (maxIndex != i)
                {
                    var temp = result[i];
                    result[i] = result[maxIndex];
                    result[maxIndex] = temp;
                }
            }

            return View(result);
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

            LaundryOrder found = null;
            foreach (var o in _context.Orders)
            {
                if (o.LaundryOrderId == id)
                {
                    found = o;
                    break;
                }
            }

            if (found == null)
            {
                return NotFound();
            }

            found.Status = status;
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

            LaundryOrder found = null;
            foreach (var o in _context.Orders)
            {
                if (o.LaundryOrderId == id)
                {
                    found = o;
                    break;
                }
            }

            if (found == null)
            {
                return NotFound();
            }

            if (!IsAdmin())
            {
                var userEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;

                if (found.UserEmail != userEmail)
                {
                    return RedirectToAction("Index");
                }
            }

            _context.Orders.Remove(found);
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
        public IActionResult CompleteOrder(int id)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return RedirectToAction("Login", "Account");
            }

            LaundryOrder found = null;
            foreach (var o in _context.Orders)
            {
                if (o.LaundryOrderId == id)
                {
                    found = o;
                    break;
                }
            }

            if (found == null)
            {
                return NotFound();
            }

            found.Status = "Completed";
            _context.Update(found);
            _context.SaveChanges();

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

            var all = new List<LaundryOrder>();
            foreach (var o in _context.Orders)
            {
                all.Add(o);
            }

            var result = new List<LaundryOrder>();
            if (IsAdmin())
            {
                // Admin sees all completed orders
                foreach (var o in all)
                {
                    if (o.Status == "Completed")
                    {
                        result.Add(o);
                    }
                }
            }
            else
            {
                var userEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
                // User sees only their own completed orders
                foreach (var o in all)
                {
                    if (o.UserEmail == userEmail && o.Status == "Completed")
                    {
                        result.Add(o);
                    }
                }
            }

            ViewBag.IsAdmin = IsAdmin();

            // Sort by OrderDate descending (explicit loops)
            for (int i = 0; i < result.Count - 1; i++)
            {
                int maxIndex = i;
                for (int j = i + 1; j < result.Count; j++)
                {
                    if (result[j].OrderDate > result[maxIndex].OrderDate)
                    {
                        maxIndex = j;
                    }
                }
                if (maxIndex != i)
                {
                    var temp = result[i];
                    result[i] = result[maxIndex];
                    result[maxIndex] = temp;
                }
            }

            return View(result);
        }
    }
}