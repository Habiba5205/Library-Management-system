using System.ComponentModel.DataAnnotations;

namespace Lib_System.Models;

public class Author
{
    public int AuthorId { get; set; }

    [Required]
    [StringLength(120)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1000)]
    public string? Biography { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();
}

