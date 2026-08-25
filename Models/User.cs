using System.ComponentModel.DataAnnotations;

namespace Lib_System.Models;

public class User
{
    public int UserId { get; set; }

    public int RoleId { get; set; }

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

    [Required]
    [StringLength(255)]
    public string PasswordHash { get; set; } = string.Empty;

    [Phone]
    [StringLength(30)]
    public string? Phone { get; set; }

    [StringLength(300)]
    public string? Address { get; set; }

    [DataType(DataType.Date)]
    public DateTime RegistrationDate { get; set; } = DateTime.Today;

    public DateTime CreatedDate { get; set; } = DateTime.Now;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Active";

    public Role? Role { get; set; }

    public ICollection<Book> ManagedBooks { get; set; } = new List<Book>();

    public ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
}

