using System.ComponentModel.DataAnnotations;

namespace Lib_System.Models;

public class Borrowing
{
    public int BorrowingId { get; set; }

    public int BookId { get; set; }

    public int UserId { get; set; }

    public DateTime? ReservationExpiresAtUtc { get; set; }

    public int LoanDays { get; set; } = 14;

    [DataType(DataType.Date)]
    public DateTime BorrowDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime DueDate { get; set; } = DateTime.Today.AddDays(14);

    [DataType(DataType.Date)]
    public DateTime? ReturnDate { get; set; }

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Borrowed";

    public Book? Book { get; set; }

    public User? User { get; set; }

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public ICollection<Fine> Fines { get; set; } = new List<Fine>();
}
