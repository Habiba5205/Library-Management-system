using Lib_System.Data;
using Lib_System.Models;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace Lib_System.Controllers
{
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Payment
        public async Task<IActionResult> Index(string? status)
        {
            var paymentsQuery = _context.Payments
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.User)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(status))
            {
                paymentsQuery = paymentsQuery.Where(p => p.Status == status);
            }

            ViewBag.Status = status;

            return View(await paymentsQuery
                .OrderByDescending(p => p.PaymentDate)
                .ThenBy(p => p.Borrowing!.Book!.Title)
                .ToListAsync());
        }

        // GET: Payment/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.User)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // GET: Payment/Create
        public async Task<IActionResult> Create(int? borrowingId)
        {
            var vm = new PaymentFormViewModel();

            if (borrowingId.HasValue)
            {
                vm.BorrowingId = borrowingId.Value;
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        // POST: Payment/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PaymentFormViewModel vm)
        {
            if (!await _context.Borrowings.AnyAsync(b => b.BorrowingId == vm.BorrowingId))
            {
                ModelState.AddModelError(nameof(vm.BorrowingId), "Select a valid borrowing record.");
            }

            if (ModelState.IsValid)
            {
                var payment = new Payment
                {
                    BorrowingId = vm.BorrowingId,
                    Amount = vm.Amount,
                    PaymentDate = vm.PaymentDate,
                    PaymentMethod = vm.PaymentMethod,
                    Status = vm.Status,
                    TransactionReference = vm.TransactionReference
                };

                _context.Payments.Add(payment);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index));
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        // GET: Payment/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments.FindAsync(id);
            if (payment == null) return NotFound();

            var vm = new PaymentFormViewModel
            {
                PaymentId = payment.PaymentId,
                BorrowingId = payment.BorrowingId,
                Amount = payment.Amount,
                PaymentDate = payment.PaymentDate,
                PaymentMethod = payment.PaymentMethod,
                Status = payment.Status,
                TransactionReference = payment.TransactionReference
            };

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        // POST: Payment/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, PaymentFormViewModel vm)
        {
            if (id != vm.PaymentId) return NotFound();

            if (!await _context.Borrowings.AnyAsync(b => b.BorrowingId == vm.BorrowingId))
            {
                ModelState.AddModelError(nameof(vm.BorrowingId), "Select a valid borrowing record.");
            }

            if (ModelState.IsValid)
            {
                var payment = await _context.Payments.FindAsync(id);
                if (payment == null) return NotFound();

                payment.BorrowingId = vm.BorrowingId;
                payment.Amount = vm.Amount;
                payment.PaymentDate = vm.PaymentDate;
                payment.PaymentMethod = vm.PaymentMethod;
                payment.Status = vm.Status;
                payment.TransactionReference = vm.TransactionReference;

                try
                {
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _context.Payments.AnyAsync(p => p.PaymentId == id)) return NotFound();
                    else throw;
                }

                return RedirectToAction(nameof(Index));
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        // GET: Payment/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _context.Payments
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.Book)
                .Include(p => p.Borrowing)
                    .ThenInclude(b => b!.User)
                .FirstOrDefaultAsync(p => p.PaymentId == id);

            if (payment == null) return NotFound();

            return View(payment);
        }

        // POST: Payment/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var payment = await _context.Payments.FindAsync(id);
            if (payment != null)
            {
                _context.Payments.Remove(payment);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBorrowingsAsync(PaymentFormViewModel vm)
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
    }
}

