using Lib_System.Data;
using Lib_System.Models;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lib_System.Controllers
{
    public class BorrowingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BorrowingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Borrowing
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Index(string? status)
        {
            var borrowingsQuery = _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .AsQueryable();

            if (User.IsInRole("Member"))
            {
                var currentUserId = GetCurrentUserId();
                borrowingsQuery = borrowingsQuery.Where(b => b.UserId == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                borrowingsQuery = borrowingsQuery.Where(b => b.Status == status);
            }

            ViewBag.Status = status;

            return View(await borrowingsQuery
                .OrderByDescending(b => b.BorrowDate)
                .ThenBy(b => b.Book!.Title)
                .ToListAsync());
        }

        // GET: Borrowing/Details/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                    .ThenInclude(book => book!.Category)
                .Include(b => b.Book)
                    .ThenInclude(book => book!.BookAuthors)
                    .ThenInclude(bookAuthor => bookAuthor.Author)
                .Include(b => b.User)
                .Include(b => b.Payments)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);

            if (borrowing == null) return NotFound();

            if (User.IsInRole("Member") && borrowing.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(borrowing);
        }

        // GET: Borrowing/Create
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager,Member")]
        public async Task<IActionResult> Create(int? bookId)
        {
            var vm = new BorrowingFormViewModel();
            if (bookId.HasValue)
            {
                vm.BookId = bookId.Value;
            }

            if (User.IsInRole("Member"))
            {
                vm.UserId = GetCurrentUserId();
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // POST: Borrowing/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager,Member")]
        public async Task<IActionResult> Create(BorrowingFormViewModel vm)
        {
            if (User.IsInRole("Member"))
            {
                vm.UserId = GetCurrentUserId();
            }

            var book = await _context.Books.FindAsync(vm.BookId);
            if (book == null)
            {
                ModelState.AddModelError(nameof(vm.BookId), "Selected book was not found.");
            }
            else if (book.AvailabilityStatus != "Available")
            {
                ModelState.AddModelError(nameof(vm.BookId), "This book is not available for borrowing.");
            }

            var memberExists = await _context.Users
                .AnyAsync(u => u.UserId == vm.UserId && u.Role != null && u.Role.RoleName == "Member");
            if (!memberExists)
            {
                ModelState.AddModelError(nameof(vm.UserId), "Select a valid library member.");
            }

            if (ModelState.IsValid)
            {
                var borrowing = new Borrowing
                {
                    BookId = vm.BookId,
                    UserId = vm.UserId,
                    BorrowDate = vm.BorrowDate,
                    DueDate = vm.BorrowDate.AddDays(vm.LoanDays),
                    Status = "Borrowed"
                };

                book!.AvailabilityStatus = "Borrowed";
                _context.Borrowings.Add(borrowing);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        // GET: Borrowing/Return/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager,Member")]
        public async Task<IActionResult> Return(int? id)
        {
            if (id == null) return NotFound();

            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);

            if (borrowing == null) return NotFound();

            if (User.IsInRole("Member") && borrowing.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(borrowing);
        }

        // POST: Borrowing/Return/5
        [HttpPost, ActionName("Return")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager,Member")]
        public async Task<IActionResult> ReturnConfirmed(int id)
        {
            var borrowing = await _context.Borrowings
                .Include(b => b.Book)
                .FirstOrDefaultAsync(b => b.BorrowingId == id);

            if (borrowing == null) return RedirectToAction(nameof(Index));

            if (User.IsInRole("Member") && borrowing.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            if (borrowing.Status == "Returned")
            {
                ModelState.AddModelError(string.Empty, "This book was already returned.");
                return View("Return", borrowing);
            }

            borrowing.ReturnDate = DateTime.Today;
            borrowing.Status = "Returned";

            if (borrowing.Book != null)
            {
                borrowing.Book.AvailabilityStatus = "Available";
            }

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id = borrowing.BorrowingId });
        }

        private async Task PopulateDropdownsAsync(BorrowingFormViewModel vm)
        {
            vm.Books = new SelectList(
                await _context.Books
                    .Where(b => b.AvailabilityStatus == "Available")
                    .OrderBy(b => b.Title)
                    .ToListAsync(),
                "BookId", "Title", vm.BookId);

            var membersQuery = _context.Users
                .Where(u => u.Role != null && u.Role.RoleName == "Member");

            if (User.IsInRole("Member"))
            {
                var currentUserId = GetCurrentUserId();
                membersQuery = membersQuery.Where(u => u.UserId == currentUserId);
            }

            vm.Members = new SelectList(
                await membersQuery.OrderBy(u => u.Name).ToListAsync(),
                "UserId", "Name", vm.UserId);
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : 0;
        }
    }
}
