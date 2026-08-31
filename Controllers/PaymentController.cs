using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lib_System.Controllers
{
    public class PaymentController : Controller
    {
        private readonly IPaymentService _paymentService;

        public PaymentController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Index(string? status)
        {
            int? restrictToUserId = User.IsInRole("Member") ? GetCurrentUserId() : null;
            var payments = await _paymentService.GetAllAsync(restrictToUserId, status);

            ViewBag.Status = status;
            return View(payments);
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _paymentService.GetDetailsAsync(id.Value);
            if (payment == null) return NotFound();

            if (User.IsInRole("Member") && payment.Borrowing?.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(payment);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(int? borrowingId)
        {
            var vm = new PaymentFormViewModel();
            if (borrowingId.HasValue) vm.BorrowingId = borrowingId.Value;

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(PaymentFormViewModel vm)
        {
            var validation = await _paymentService.ValidateBorrowingAsync(vm.BorrowingId);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                await _paymentService.CreateAsync(vm);
                return RedirectToAction(nameof(Index));
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _paymentService.GetForEditAsync(id.Value);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, PaymentFormViewModel vm)
        {
            if (id != vm.PaymentId) return NotFound();

            var validation = await _paymentService.ValidateBorrowingAsync(vm.BorrowingId);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                bool updated;
                try
                {
                    updated = await _paymentService.UpdateAsync(id, vm);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _paymentService.ExistsAsync(id)) return NotFound();
                    else throw;
                }

                if (!updated) return NotFound();
                return RedirectToAction(nameof(Index));
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null) return NotFound();

            var payment = await _paymentService.GetForDeleteAsync(id.Value);
            if (payment == null) return NotFound();

            return View(payment);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _paymentService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBorrowingsAsync(PaymentFormViewModel vm)
        {
            var borrowings = await _paymentService.GetBorrowingsForDropdownAsync();

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