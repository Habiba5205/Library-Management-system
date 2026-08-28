using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lib_System.ViewModels
{
    public class UserFormViewModel
    {
        public int UserId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(150)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;

        // Plaintext password entered by the user. Required on Create.
        // Left blank on Edit means "keep the current password".
        // This is NEVER stored directly - the controller hashes it into
        // User.PasswordHash via PasswordHasher<User> and this field is
        // discarded after that.
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string? Password { get; set; }

        [Phone]
        [StringLength(30)]
        public string? Phone { get; set; }

        [StringLength(300)]
        public string? Address { get; set; }

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Active";

        [Required]
        [Display(Name = "Role")]
        public int RoleId { get; set; }

        // Populated by the controller, not posted back from the browser.
        public SelectList? Roles { get; set; }
    }
}
