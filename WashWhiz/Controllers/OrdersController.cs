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

        // -----------------------
        // New: email suggestions (admin autocomplete)
        // -----------------------
        [HttpGet]
        [Route("EmailSuggestions")]
        public IActionResult EmailSuggestions(string q)
        {
            var suggestions = new List<string>();

            if (string.IsNullOrEmpty(q))
            {
                return Json(suggestions);
            }

            var query = q.Trim();

            // explicit loop for curriculum alignment
            foreach (var u in _context.Users)
            {
                if (string.IsNullOrEmpty(u.Email))
                {
                    continue;
                }

                // case-insensitive substring match
                if (u.Email.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    suggestions.Add(u.Email);
                }
            }

            return Json(suggestions);
        }

        // get order details for payment modal
        [HttpGet]
        [Route("GetOrderDetails")]
        public IActionResult GetOrderDetails(int id)
        {
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

            var fee = found.Weight * 30m;

            var result = new
            {
                id = found.LaundryOrderId,
                customerName = found.CustomerName,
                email = found.UserEmail,
                weight = found.Weight,
                serviceType = found.ServiceType,
                status = found.Status,
                paid = found.Paid,
                paymentAmount = found.PaymentAmount,
                // Friendly formatted date for receipts (e.g. May 13, 2026 02:35 PM)
                paymentDate = found.PaymentDate.HasValue ? found.PaymentDate.Value.ToString("MMMM dd, yyyy hh:mm tt") : null,
                fee = fee
            };

            return Json(result);
        }

        // -----------------------
        // New: process payment (admin)
        // -----------------------
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("PayOrder")]
        public IActionResult PayOrder(int id, decimal cashReceived)
        {
            var userRole = HttpContext.Session.GetString("UserRole");
            if (userRole != "Admin")
            {
                return Forbid();
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

            var fee = found.Weight * 30m;
            var change = cashReceived - fee;

            found.Paid = true;
            found.PaymentAmount = cashReceived;
            found.PaymentDate = DateTime.Now;

            _context.SaveChanges();

            var receipt = new
            {
                orderId = found.LaundryOrderId,
                customerName = found.CustomerName,
                email = found.UserEmail,
                weight = found.Weight,
                fee = fee,
                cashReceived = cashReceived,
                change = change,
                // Friendly formatted date
                paymentDate = found.PaymentDate.HasValue ? found.PaymentDate.Value.ToString("MMMM dd, yyyy hh:mm tt") : null
            };

            return Json(new { success = true, receipt = receipt });
        }

        [HttpGet]
        [Route("GetReceipt")]
        public IActionResult GetReceipt(int id)
        {
            // Get the order from the database using the provided ID
            var order = _context.Orders.FirstOrDefault(o => o.LaundryOrderId == id);

            if (order == null)
            {
                return NotFound(); // If the order is not found, return a 404 error.
            }

            // Check if the current user is authorized to view the receipt (based on role and ownership)
            string userEmail = HttpContext.Session.GetString("UserEmail") ?? string.Empty;
            string userRole = HttpContext.Session.GetString("UserRole");

            if ((userRole == "Admin") || (userEmail == order.UserEmail))
            {
                // Generate receipt details
                var receiptDetails = new
                {
                    OrderId = order.LaundryOrderId,
                    CustomerName = order.CustomerName,
                    Email = order.UserEmail,
                    ServiceType = order.ServiceType,
                    Weight = order.Weight,
                    Status = order.Status,
                    Fee = order.Weight * 30m,
                    PaymentAmount = order.PaymentAmount,
                    PaymentDate = order.PaymentDate?.ToString("MMMM dd, yyyy hh:mm tt")
                };

                // Return the receipt view with the details
                return View("Receipt", receiptDetails);
            }

            return Forbid(); // If the user is not authorized, return a forbidden response.
        }
    }
}