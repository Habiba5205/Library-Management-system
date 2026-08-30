using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lib_System.ViewModels
{
    public class FineFormViewModel
    {
        public int FineId { get; set; }

        [Required]
        [Display(Name = "Borrowing")]
        public int BorrowingId { get; set; }

        [Range(0.01, 999999)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Fine Date")]
        public DateTime FineDate { get; set; } = DateTime.Today;

        [Required]
        [StringLength(250)]
        public string Reason { get; set; } = "Late return";

        [Required]
        [StringLength(30)]
        public string Status { get; set; } = "Unpaid";

        public SelectList? Borrowings { get; set; }
    }
}

