using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Lib_System.Models;

namespace Lib_System.ViewModels
{
    public class BookFormViewModel
    {
        public int BookId { get; set; }

        [Required]
        [StringLength(20)]
        [Display(Name = "ISBN")]
        public string ISBN { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Range(1000, 9999)]
        [Display(Name = "Publication Year")]
        public int PublicationYear { get; set; } = DateTime.Now.Year;

        [Range(0, 999999)]
        public decimal Price { get; set; }

        [Required]
        [StringLength(30)]
        [Display(Name = "Availability Status")]
        public string AvailabilityStatus { get; set; } = "Available";

        [Required]
        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        [Display(Name = "Manager")]
        public int? ManagerId { get; set; }

        // Display-only, populated by the controller from the book's current manager.
        // Not editable - ManagerId is always set server-side (the logged-in manager
        // on Create, left unchanged on Edit).
        public string? ManagerName { get; set; }

        [Display(Name = "Authors")]
        public List<int> SelectedAuthorIds { get; set; } = new();

        // Populated by the controller to build the form's dropdowns/checkboxes.
        // Not posted back from the browser.
        public SelectList? Categories { get; set; }
        public List<Author> AllAuthors { get; set; } = new();
    }
}
