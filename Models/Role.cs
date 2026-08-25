using System.ComponentModel.DataAnnotations;

namespace Lib_System.Models;

public class Role
{
    public int RoleId { get; set; }

    [Required]
    [StringLength(50)]
    public string RoleName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Description { get; set; }

    [StringLength(500)]
    public string? Permissions { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
}

