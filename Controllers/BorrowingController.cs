using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Security.Claims;

namespace Lib_System.Controllers
{
    public class BorrowingController : Controller
    {
        private readonly IBorrowingService _borrowingService;

        public BorrowingController(IBorrowingService borrowingService)
        {
            _borrowingService = borrowingService;
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Index(string? status)
        {
            int? restrictToUserId = User.IsInRole("Member") ? GetCurrentUserId() : null;
            var borrowings = await _borrowingService.GetBorrowingsAsync(restrictToUserId, status);

            ViewBag.Status = status;
            return View(borrowings);
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var borrowing = await _borrowingService.GetBorrowingDetailsAsync(id.Value);
            if (borrowing == null) return NotFound();

            if (User.IsInRole("Member") && borrowing.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(borrowing);
        }

        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Create(int? bookId)
        {
            var vm = new BorrowingFormViewModel();

            if (bookId.HasValue) vm.BookId = bookId.Value;
            vm.UserId = GetCurrentUserId();

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Member")]
        public async Task<IActionResult> Create(BorrowingFormViewModel vm)
        {
            if (vm.PaymentMethod == "Card" && !HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
                ModelState.AddModelError("PaymentMethod", "Card checkout is currently available in the development demo only.");
            // Always the logged-in member - never trust a UserId posted from the form.
            vm.UserId = GetCurrentUserId();

            var validation = await _borrowingService.ValidateForCreateAsync(vm.BookId, vm.UserId);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                var paymentId = await _borrowingService.CreateAsync(vm.BookId, vm.UserId, vm.BorrowDate, vm.LoanDays, vm.PaymentMethod);
                if (paymentId.HasValue)
                    return RedirectToAction(vm.PaymentMethod == "Card" ? "Checkout" : "Details", "Payment", new { id = paymentId.Value });
                ModelState.AddModelError("", "The book could not be reserved. It may have just been reserved by another member.");
            }

            await PopulateDropdownsAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Manager,Member")]
        public async Task<IActionResult> Return(int? id)
        {
            if (id == null) return NotFound();

            var borrowing = await _borrowingService.GetReturnCandidateAsync(id.Value);
            if (borrowing == null) return NotFound();

            if (User.IsInRole("Member") && borrowing.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(borrowing);
        }

        [HttpPost, ActionName("Return")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager,Member")]
        public async Task<IActionResult> ReturnConfirmed(int id)
        {
            var borrowing = await _borrowingService.GetReturnCandidateAsync(id);
            if (borrowing == null) return RedirectToAction(nameof(Index));

            if (User.IsInRole("Member") && borrowing.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            var result = User.IsInRole("Member") && DateTime.Today < borrowing.DueDate.Date
                ? await _borrowingService.RequestEarlyReturnAsync(id)
                : await _borrowingService.ReturnBorrowingAsync(id);

            if (!result.Success)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(error.Field, error.Message);
                }
                return View("Return", borrowing);
            }

            return RedirectToAction(nameof(Details), new { id = borrowing.BorrowingId });
        }

        private async Task PopulateDropdownsAsync(BorrowingFormViewModel vm)
        {
            var availableBooks = await _borrowingService.GetAvailableBooksAsync();
            vm.Books = new SelectList(availableBooks, "BookId", "Title", vm.BookId);
        }

        private int GetCurrentUserId()
        {
            var value = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(value, out var userId) ? userId : 0;
        }
    }
}
