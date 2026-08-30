using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Lib_System.Data;
using Lib_System.Models;
using Lib_System.ViewModels;

namespace Lib_System.Controllers
{
    public class BookController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Book
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Index(string? searchString, int? categoryId, int? authorId)
        {
            var booksQuery = _context.Books
                .Include(b => b.Category)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchString))
            {
                booksQuery = booksQuery.Where(b =>
                    b.Title.Contains(searchString) ||
                    b.ISBN.Contains(searchString));
            }

            if (categoryId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.CategoryId == categoryId.Value);
            }

            if (authorId.HasValue)
            {
                booksQuery = booksQuery.Where(b => b.BookAuthors.Any(ba => ba.AuthorId == authorId.Value));
            }

            if (User.IsInRole("Member"))
            {
                booksQuery = booksQuery.Where(b => b.AvailabilityStatus == "Available");
            }

            ViewBag.Categories = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "CategoryId", "Name", categoryId);
            ViewBag.Authors = new SelectList(
                await _context.Authors.OrderBy(a => a.Name).ToListAsync(), "AuthorId", "Name", authorId);
            ViewBag.SearchString = searchString;

            return View(await booksQuery.OrderBy(b => b.Title).ToListAsync());
        }

        // GET: Book/Details/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Manager)
                .Include(b => b.BookAuthors)
                    .ThenInclude(ba => ba.Author)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null) return NotFound();

            if (User.IsInRole("Member") && book.AvailabilityStatus != "Available")
            {
                return Forbid();
            }

            return View(book);
        }

        // GET: Book/Create
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create()
        {
            var vm = new BookFormViewModel();
            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // POST: Book/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(BookFormViewModel vm)
        {
            if (vm.SelectedAuthorIds == null || vm.SelectedAuthorIds.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.SelectedAuthorIds), "Select at least one author.");
            }

            if (await _context.Books.AnyAsync(b => b.ISBN == vm.ISBN))
            {
                ModelState.AddModelError(nameof(vm.ISBN), "This ISBN is already in use.");
            }

            if (ModelState.IsValid)
            {
                var book = new Book
                {
                    ISBN = vm.ISBN,
                    Title = vm.Title,
                    PublicationYear = vm.PublicationYear,
                    Price = vm.Price,
                    AvailabilityStatus = vm.AvailabilityStatus,
                    CategoryId = vm.CategoryId,
                    ManagerId = vm.ManagerId
                };

                foreach (var authorId in vm.SelectedAuthorIds!)
                {
                    book.BookAuthors.Add(new BookAuthor { AuthorId = authorId });
                }

                _context.Add(book);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // GET: Book/Edit/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.BookAuthors)
                .FirstOrDefaultAsync(b => b.BookId == id);

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
                SelectedAuthorIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList()
            };

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // POST: Book/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, BookFormViewModel vm)
        {
            if (id != vm.BookId) return NotFound();

            if (vm.SelectedAuthorIds == null || vm.SelectedAuthorIds.Count == 0)
            {
                ModelState.AddModelError(nameof(vm.SelectedAuthorIds), "Select at least one author.");
            }

            if (await _context.Books.AnyAsync(b => b.ISBN == vm.ISBN && b.BookId != id))
            {
                ModelState.AddModelError(nameof(vm.ISBN), "This ISBN is already in use.");
            }

            if (ModelState.IsValid)
            {
                var book = await _context.Books
                    .Include(b => b.BookAuthors)
                    .FirstOrDefaultAsync(b => b.BookId == id);

                if (book == null) return NotFound();

                book.ISBN = vm.ISBN;
                book.Title = vm.Title;
                book.PublicationYear = vm.PublicationYear;
                book.Price = vm.Price;
                book.AvailabilityStatus = vm.AvailabilityStatus;
                book.CategoryId = vm.CategoryId;
                book.ManagerId = vm.ManagerId;

                // Sync the BookAuthors join rows with the selected author list.
                var toRemove = book.BookAuthors
                    .Where(ba => !vm.SelectedAuthorIds!.Contains(ba.AuthorId))
                    .ToList();
                foreach (var ba in toRemove)
                {
                    book.BookAuthors.Remove(ba);
                }

                var existingIds = book.BookAuthors.Select(ba => ba.AuthorId).ToList();
                foreach (var authorId in vm.SelectedAuthorIds!.Except(existingIds))
                {
                    book.BookAuthors.Add(new BookAuthor { AuthorId = authorId, BookId = book.BookId });
                }

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Books.AnyAsync(b => b.BookId == id)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // GET: Book/Delete/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null) return NotFound();

            return View(book);
        }

        // POST: Book/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var book = await _context.Books
                .Include(b => b.Category)
                .Include(b => b.Borrowings)
                .FirstOrDefaultAsync(b => b.BookId == id);

            if (book == null) return RedirectToAction(nameof(Index));

            if (book.Borrowings.Any())
            {
                ModelState.AddModelError(string.Empty,
                    "This book cannot be deleted because it has borrowing history. Resolve or remove the related borrowings first.");
                return View("Delete", book);
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateDropdownsAsync(BookFormViewModel vm)
        {
            vm.Categories = new SelectList(
                await _context.Categories.OrderBy(c => c.Name).ToListAsync(), "CategoryId", "Name", vm.CategoryId);

            vm.Managers = new SelectList(
                await _context.Users
                    .Where(u => u.Role != null && u.Role.RoleName == "Manager")
                    .OrderBy(u => u.Name)
                    .ToListAsync(),
                "UserId", "Name", vm.ManagerId);

            vm.AllAuthors = await _context.Authors.OrderBy(a => a.Name).ToListAsync();
        }
    }
}
