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

        [Required]
        [AllowedValues("Cash", "Card", ErrorMessage = "Select Cash or Card.")]
        [Display(Name = "Payment Method")]
        public string PaymentMethod { get; set; } = string.Empty;

        public SelectList? Books { get; set; }
    }
}
