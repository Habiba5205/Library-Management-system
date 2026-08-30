using Lib_System.Data;
using Lib_System.Models;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lib_System.Controllers
{
    public class FineController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FineController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Fine
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Index(string? status)
        {
            var finesQuery = _context.Fines
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.User)
                .AsQueryable();

            if (User.IsInRole("Member"))
            {
                var currentUserId = GetCurrentUserId();
                finesQuery = finesQuery.Where(f => f.Borrowing != null && f.Borrowing.UserId == currentUserId);
            }

            if (!string.IsNullOrWhiteSpace(status))
            {
                finesQuery = finesQuery.Where(f => f.Status == status);
            }

            ViewBag.Status = status;

            return View(await finesQuery
                .OrderByDescending(f => f.FineDate)
                .ThenBy(f => f.Borrowing!.Book!.Title)
                .ToListAsync());
        }

        // GET: Fine/Details/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var fine = await _context.Fines
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.User)
                .FirstOrDefaultAsync(f => f.FineId == id);

            if (fine == null) return NotFound();

            if (User.IsInRole("Member") && fine.Borrowing?.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(fine);
        }

        // GET: Fine/Create
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(int? borrowingId)
        {
            var vm = new FineFormViewModel();

            if (borrowingId.HasValue)
            {
                vm.BorrowingId = borrowingId.Value;
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        // POST: Fine/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(FineFormViewModel vm)
        {
            if (!await _context.Borrowings.AnyAsync(b => b.BorrowingId == vm.BorrowingId))
            {
                ModelState.AddModelError(nameof(vm.BorrowingId), "Select a valid borrowing record.");
            }

            if (ModelState.IsValid)
            {
                var fine = new Fine
                {
                    BorrowingId = vm.BorrowingId,
                    Amount = vm.Amount,
                    FineDate = vm.FineDate,
                    Reason = vm.Reason,
                    Status = vm.Status
                };

                _context.Fines.Add(fine);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        // GET: Fine/Edit/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var fine = await _context.Fines.FindAsync(id);
            if (fine == null) return NotFound();

            var vm = new FineFormViewModel
            {
                FineId = fine.FineId,
                BorrowingId = fine.BorrowingId,
                Amount = fine.Amount,
                FineDate = fine.FineDate,
                Reason = fine.Reason,
                Status = fine.Status
            };

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        // POST: Fine/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, FineFormViewModel vm)
        {
            if (id != vm.FineId) return NotFound();

            if (!await _context.Borrowings.AnyAsync(b => b.BorrowingId == vm.BorrowingId))
            {
                ModelState.AddModelError(nameof(vm.BorrowingId), "Select a valid borrowing record.");
            }

            if (ModelState.IsValid)
            {
                var fine = await _context.Fines.FindAsync(id);
                if (fine == null) return NotFound();

                fine.BorrowingId = vm.BorrowingId;
                fine.Amount = vm.Amount;
                fine.FineDate = vm.FineDate;
                fine.Reason = vm.Reason;
                fine.Status = vm.Status;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Fines.AnyAsync(f => f.FineId == id)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        // GET: Fine/Delete/5
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var fine = await _context.Fines
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(f => f.Borrowing)
                    .ThenInclude(b => b!.User)
                .FirstOrDefaultAsync(f => f.FineId == id);

            if (fine == null) return NotFound();

            return View(fine);
        }

        // POST: Fine/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var fine = await _context.Fines.FindAsync(id);
            if (fine != null)
            {
                _context.Fines.Remove(fine);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBorrowingsAsync(FineFormViewModel vm)
        {
            var borrowings = await _context.Borrowings
                .Include(b => b.Book)
                .Include(b => b.User)
                .OrderByDescending(b => b.BorrowDate)
                .ToListAsync();

            vm.Borrowings = new SelectList(
                borrowings.Select(b => new
                {
                    b.BorrowingId,
                    Label = $"#{b.BorrowingId} - {b.Book?.Title ?? "Unknown book"} / {b.User?.Name ?? "Unknown member"}"
                }),
                "BorrowingId", "Label", vm.BorrowingId);
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : 0;
        }
    }
}

