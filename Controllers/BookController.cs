using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lib_System.Controllers
{
    public class BookController : Controller
    {
        private readonly IBookService _bookService;

        public BookController(IBookService bookService)
        {
            _bookService = bookService;
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Index(string? searchString, int? categoryId, int? authorId)
        {
            bool memberOnly = User.IsInRole("Member");
            var books = await _bookService.GetAllAsync(searchString, categoryId, authorId, memberOnly);

            var categories = await _bookService.GetCategoriesAsync();
            var authors = await _bookService.GetAllAuthorsAsync();

            ViewBag.Categories = new SelectList(categories, "CategoryId", "Name", categoryId);
            ViewBag.Authors = new SelectList(authors, "AuthorId", "Name", authorId);
            ViewBag.SearchString = searchString;

            return View(books);
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetDetailsAsync(id.Value);
            if (book == null) return NotFound();

            if (User.IsInRole("Member") && book.AvailabilityStatus != "Available")
            {
                return Forbid();
            }

            return View(book);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create()
        {
            var vm = new BookFormViewModel();
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(BookFormViewModel vm)
        {
            // Always the logged-in manager - never trust a ManagerId posted from the form.
            vm.ManagerId = GetCurrentUserId();

            var validation = await _bookService.ValidateForCreateAsync(vm);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                await _bookService.CreateAsync(vm);
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetForEditAsync(id.Value);
            if (book == null) return NotFound();

            var vm = new BookFormViewModel
            {
                BookId = book.BookId,
                ISBN = book.ISBN,
                Title = book.Title,
                PublicationYear = book.PublicationYear,
                Price = book.Price,
                AvailabilityStatus = book.AvailabilityStatus,
                CategoryId = book.CategoryId,
                ManagerId = book.ManagerId,
                ManagerName = book.Manager?.Name ?? "Unassigned",
                SelectedAuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList()
            };

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, BookFormViewModel vm)
        {
            if (id != vm.BookId) return NotFound();

            var validation = await _bookService.ValidateForEditAsync(id, vm);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                bool updated;
                try
                {
                    updated = await _bookService.UpdateAsync(id, vm);
                }
                catch (InvalidOperationException ex)
                {
                    ModelState.AddModelError(nameof(vm.AvailabilityStatus), ex.Message);
                    await PopulateDropdownsAsync(vm);
                    return View(vm);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _bookService.ExistsAsync(id)) return NotFound();
                    else throw;
                }

                if (!updated) return NotFound();
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _bookService.GetForDeleteAsync(id.Value);
            if (book == null) return NotFound();

            return View(book);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _bookService.GetForDeleteAsync(id);
            if (book == null) return RedirectToAction(nameof(Index));

            var result = await _bookService.DeleteAsync(id);
            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Field, error.Message);
                }
                return View("Delete", book);
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(BookFormViewModel vm)
        {
            var categories = await _bookService.GetCategoriesAsync();
            vm.Categories = new SelectList(categories, "CategoryId", "Name", vm.CategoryId);

            vm.AllAuthors = await _bookService.GetAllAuthorsAsync();
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : 0;
        }
    }
}
