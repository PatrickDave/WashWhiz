using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WashWhiz.Models
{
    public class UserAccount
    {
        [Key]
        public int UserId { get; set; }

        public string Username { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [Phone]
        public string? PhoneNumber { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        // ConfirmPassword is not mapped to the DB; used only for validation on registration
        [NotMapped]
        [Compare("Password", ErrorMessage = "Passwords do not match.")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; } = string.Empty;

        public string ProfilePicture { get; set; } = "default.png";

        public string Role { get; set; } = "User";
    }
}