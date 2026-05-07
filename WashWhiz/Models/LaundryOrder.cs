using System;
using System.ComponentModel.DataAnnotations;

namespace WashWhiz.Models
{
    public class LaundryOrder
    {
        public int LaundryOrderId { get; set; }

        // This connects the order to the User
        public string UserId { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string UserEmail { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;
        public decimal Weight { get; set; }
        public string ServiceType { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // Pending, Washing, Ready, Completed

        // For tracking when the load was dropped off
        public DateTime OrderDate { get; set; } = DateTime.Now;

        // Some controllers/views expect DateCreated
        public DateTime DateCreated { get; set; } = DateTime.Now;
    }
}