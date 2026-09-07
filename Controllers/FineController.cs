using Lib_System.Services;
using Lib_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lib_System.Controllers;

[Authorize(Roles = "Admin,Manager,Member")]
public class FineController(IFineService fines, IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(string? status)
    {
        ViewBag.Status = status;
        return View(await fines.GetAllAsync(User.IsInRole("Member") ? CurrentUserId() : null, status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var fine = await fines.GetDetailsAsync(id);
        if (fine == null) return NotFound();
        if (User.IsInRole("Member") && fine.Borrowing?.UserId != CurrentUserId()) return Forbid();
        return View(fine);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Member")]
    public async Task<IActionResult> Pay(int id, string method, decimal amount)
    {
        if (method == "Card" && !environment.IsDevelopment()) return NotFound();
        var result = await fines.StartPaymentAsync(id, CurrentUserId(), method, amount);
        if (!result.Success)
        {
            SetMessage(result);
            return RedirectToAction(nameof(Details), new { id });
        }
        if (method == "Cash") TempData["FineMessage"] = "Pay the manager in person and hand over the book. Your fine remains unpaid until confirmation.";
        return RedirectToAction(method == "Card" ? nameof(Checkout) : nameof(Details), new { id });
    }

    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Checkout(int id)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var fine = await fines.GetDetailsAsync(id);
        if (fine == null) return NotFound();
        if (fine.Borrowing?.UserId != CurrentUserId()) return Forbid();
        if (fine.Status != "Unpaid" || fine.PaymentMethod != "Card" || fine.PaymentStatus != "Pending")
            return RedirectToAction(nameof(Details), new { id });
        return View(fine);
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Member")]
    public async Task<IActionResult> DemoResult(int id, Guid attemptId, decimal amount, string outcome)
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (outcome is not ("success" or "failure" or "cancel")) return BadRequest();
        var result = await fines.CompletePaymentAsync(id, CurrentUserId(), "Card", attemptId, amount, outcome == "success");
        SetMessage(result, outcome == "success" ? "Demo payment completed. The book is returned once all its fines are paid."
            : "Payment was not completed. The fine is still unpaid and you can retry.");
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = "Manager")]
    public async Task<IActionResult> ConfirmCash(int id, Guid attemptId, decimal amount)
    {
        SetMessage(await fines.CompletePaymentAsync(id, null, "Cash", attemptId, amount, true),
            "Cash received. The book is returned once all its fines are paid.");
        return RedirectToAction(nameof(Details), new { id });
    }

    private void SetMessage(ServiceResult result, string success = "") =>
        TempData["FineMessage"] = result.Success ? success : string.Join(" ", result.Errors.Select(e => e.Message));

    private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
