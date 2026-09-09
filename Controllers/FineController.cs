using Lib_System.Services;
using Lib_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lib_System.Controllers;

[Authorize(Roles = "Admin,Manager,Member")]
public class FineController(IFineService fines, IStripeCheckoutService stripe) : Controller
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
        var result = await fines.StartPaymentAsync(id, CurrentUserId(), method, amount);
        if (!result.Success)
        {
            SetMessage(result);
            return RedirectToAction(nameof(Details), new { id });
        }
        if (method == "Cash") TempData["FineMessage"] = "Pay the manager in person and hand over the book. Your fine remains unpaid until confirmation.";
        return RedirectToAction(method == "Card" ? nameof(Checkout) : nameof(Details), new { id });
    }

    /// <summary>Starts a real Stripe Checkout Session and sends the member there to pay the fine.</summary>
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Checkout(int id)
    {
        var fine = await fines.GetDetailsAsync(id);
        if (fine == null) return NotFound();
        if (fine.Borrowing?.UserId != CurrentUserId()) return Forbid();
        if (fine.Status != "Unpaid" || fine.PaymentMethod != "Card" || fine.PaymentStatus != "Pending")
            return RedirectToAction(nameof(Details), new { id });

        if (!stripe.IsConfigured)
        {
            TempData["FineMessage"] = "Card payments aren't set up yet. Ask an administrator to configure Stripe.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var successUrl = Url.Action(nameof(StripeReturn), "Fine", new { id }, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(Details), "Fine", new { id }, Request.Scheme)!;
        var sessionUrl = await stripe.CreateFinePaymentSessionAsync(fine, successUrl, cancelUrl);
        return Redirect(sessionUrl);
    }

    /// <summary>Where Stripe sends the member back after Checkout (see PaymentController.StripeReturn for why this also reconciles, not just the webhook).</summary>
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> StripeReturn(int id, string? session_id)
    {
        if (!string.IsNullOrEmpty(session_id))
            await stripe.HandleCompletedSessionAsync(session_id);

        var fine = await fines.GetDetailsAsync(id);
        TempData["FineMessage"] = fine?.Status == "Paid"
            ? "Payment succeeded. The book is returned once all its fines are paid."
            : "We're still confirming your payment with Stripe - refresh in a moment if the status doesn't update.";
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
