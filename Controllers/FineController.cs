using Lib_System.Services.Interfaces;
using Lib_System.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace Lib_System.Controllers
{
    public class FineController : Controller
    {
        private readonly IFineService _fineService;

        public FineController(IFineService fineService)
        {
            _fineService = fineService;
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Index(string? status)
        {
            int? restrictToUserId = User.IsInRole("Member") ? GetCurrentUserId() : null;
            var fines = await _fineService.GetAllAsync(restrictToUserId, status);

            ViewBag.Status = status;
            return View(fines);
        }

        [Authorize(Roles = "Admin,Manager,Member")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var fine = await _fineService.GetDetailsAsync(id.Value);
            if (fine == null) return NotFound();

            if (User.IsInRole("Member") && fine.Borrowing?.UserId != GetCurrentUserId())
            {
                return Forbid();
            }

            return View(fine);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(int? borrowingId)
        {
            var vm = new FineFormViewModel();
            if (borrowingId.HasValue) vm.BorrowingId = borrowingId.Value;

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Create(FineFormViewModel vm)
        {
            var validation = await _fineService.ValidateBorrowingAsync(vm.BorrowingId);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                await _fineService.CreateAsync(vm);
                return RedirectToAction(nameof(Index));
            }

            await PopulateBorrowingsAsync(vm);
            return View(vm);
        }

        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var fine = await _fineService.GetForEditAsync(id.Value);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> Edit(int id, FineFormViewModel vm)
        {
            if (id != vm.FineId) return NotFound();

            var validation = await _fineService.ValidateBorrowingAsync(vm.BorrowingId);
            foreach (var error in validation.Errors)
            {
                ModelState.AddModelError(error.Field, error.Message);
            }

            if (ModelState.IsValid)
            {
                bool updated;
                try
                {
                    updated = await _fineService.UpdateAsync(id, vm);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _fineService.ExistsAsync(id)) return NotFound();
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

            var fine = await _fineService.GetForDeleteAsync(id.Value);
            if (fine == null) return NotFound();

            return View(fine);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Manager")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await _fineService.DeleteAsync(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task PopulateBorrowingsAsync(FineFormViewModel vm)
        {
            var borrowings = await _fineService.GetBorrowingsForDropdownAsync();

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