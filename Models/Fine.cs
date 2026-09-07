using System.ComponentModel.DataAnnotations;

namespace Lib_System.Models;

public class Fine
{
    public int FineId { get; set; }

    public int BorrowingId { get; set; }
    public bool IsAutomatic { get; set; }
    [StringLength(10)]
    public string? PaymentMethod { get; set; }
    public decimal? PaymentAmount { get; set; }
    public Guid? PaymentAttemptId { get; set; }
    [StringLength(20)]
    public string? PaymentStatus { get; set; }
    public DateTime? PaidAtUtc { get; set; }

    [Range(0.01, 999999)]
    public decimal Amount { get; set; }

    [DataType(DataType.Date)]
    public DateTime FineDate { get; set; } = DateTime.Today;

    [Required]
    [StringLength(250)]
    public string Reason { get; set; } = "Late return";

    [Required]
    [StringLength(30)]
    public string Status { get; set; } = "Unpaid";

    public Borrowing? Borrowing { get; set; }
}
