using System.ComponentModel.DataAnnotations;

namespace Lib_System.Models;

public class Book
{
    public int BookId { get; set; }

    [Required]
    [StringLength(20)]
    public string ISBN { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    public byte[]? CoverImage { get; set; }

    [Range(1000, 9999)]
    public int PublicationYear { get; set; }

    [Range(0, 999999)]
    public decimal Price { get; set; }

    [Required]
    [StringLength(30)]
    public string AvailabilityStatus { get; set; } = "Available";

    public int CategoryId { get; set; }

    public int? ManagerId { get; set; }

    public Category? Category { get; set; }

    public User? Manager { get; set; }

    public ICollection<BookAuthor> BookAuthors { get; set; } = new List<BookAuthor>();

    public ICollection<Borrowing> Borrowings { get; set; } = new List<Borrowing>();
}
