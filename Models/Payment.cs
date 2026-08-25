using System.ComponentModel.DataAnnotations;

namespace Lib_System.Models;

public class Payment
{
    public int PaymentId { get; set; }

    public int BorrowingId { get; set; }

    [Range(0, 999999)]
    public decimal Amount { get; set; }

    public DateTime PaymentDate { get; set; } = DateTime.Now;

    [Required]
    [StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Pending";

    [StringLength(100)]
    public string? TransactionReference { get; set; }

    public Borrowing? Borrowing { get; set; }
}

