using Lib_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lib_System.Controllers;

[Authorize(Roles = "Admin,Manager,Member")]
public class PaymentController(IPaymentService payments, IWebHostEnvironment environment) : Controller
{
    public async Task<IActionResult> Index(string? status)
    {
        ViewBag.Status = status;
        return View(await payments.GetAllAsync(User.IsInRole("Member") ? CurrentUserId() : null, status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var payment = await payments.GetDetailsAsync(id);
        if (payment == null) return NotFound();
        if (User.IsInRole("Member") && payment.Borrowing?.UserId != CurrentUserId()) return Forbid();
        return View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Manager")]
    public async Task<IActionResult> ConfirmCash(int id)
    {
        var confirmed = await payments.ConfirmCashAsync(id);
        TempData["PaymentMessage"] = confirmed ? "Cash received. The borrowing is now active."
            : "Cash could not be confirmed. The reservation may have expired.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Checkout(int id)
    {
        if (!environment.IsDevelopment()) return NotFound();
        var payment = await payments.GetDetailsAsync(id);
        if (payment == null) return NotFound();
        if (payment.Borrowing?.UserId != CurrentUserId() || payment.PaymentMethod != "Card") return Forbid();
        if (payment.Status != "Pending") return RedirectToAction(nameof(Details), new { id });
        return View(payment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> DemoResult(int id, string outcome)
    {
        if (!environment.IsDevelopment()) return NotFound();
        if (outcome is not ("success" or "failure" or "cancel")) return BadRequest();
        var completed = await payments.CompleteDemoAsync(id, CurrentUserId(), outcome == "success");
        if (!completed)
            TempData["PaymentMessage"] = "The payment could not be completed. The reservation may have expired.";
        else
            TempData["PaymentMessage"] = outcome == "success" ? "Demo payment succeeded. Your borrowing is active."
                : "Payment was not completed. Your borrowing request failed and the book is available again.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
