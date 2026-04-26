using System.ComponentModel.DataAnnotations;

namespace WashWhiz.Models
{
    public class LaundryOrder
    {
        [Key]
        public int LaundryOrderId { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        [Range(0.1, 100)]
        public double Weight { get; set; }

        [Required]
        public string ServiceType { get; set; } = string.Empty;

        public string Status { get; set; } = "Pending";

        public DateTime DateCreated { get; set; } = DateTime.Now;

        public int UserId { get; set; }
    }
}