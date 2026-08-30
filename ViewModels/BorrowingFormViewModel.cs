using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace Lib_System.ViewModels
{
    public class BorrowingFormViewModel
    {
        [Required]
        [Display(Name = "Book")]
        public int BookId { get; set; }

        [Required]
        [Display(Name = "Member")]
        public int UserId { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Borrow Date")]
        public DateTime BorrowDate { get; set; } = DateTime.Today;

        [Range(1, 60)]
        [Display(Name = "Loan Days")]
        public int LoanDays { get; set; } = 14;

        public SelectList? Books { get; set; }

        public SelectList? Members { get; set; }
    }
}

