using System.ComponentModel.DataAnnotations;

namespace Lib_System.ViewModels
{
    public class LoginViewModel
    {
        [Required]
        [Display(Name = "Username or Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}

