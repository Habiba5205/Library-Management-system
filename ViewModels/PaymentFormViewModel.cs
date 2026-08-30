using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lib_System.ViewModels
{
    public class PaymentFormViewModel
    {
        public int PaymentId { get; set; }

        [Required]
        [Display(Name = "Borrowing")]
        public int BorrowingId { get; set; }

        [Range(0.01, 999999)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(50)]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = "Cash";

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Paid";

        [StringLength(100)]
        [Display(Name = "Transaction Reference")]
        public string? TransactionReference { get; set; }

        public SelectList? Borrowings { get; set; }
    }
}

