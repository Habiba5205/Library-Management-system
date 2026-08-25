using System.ComponentModel.DataAnnotations;

namespace Lib_System.Models;

public class Category
{
    public int CategoryId { get; set; }

    [Required]
    [StringLength(80)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string? Description { get; set; }

    public ICollection<Book> Books { get; set; } = new List<Book>();
}

