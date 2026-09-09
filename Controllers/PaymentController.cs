using Lib_System.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Lib_System.Controllers;

[Authorize(Roles = "Admin,Manager,Member")]
public class PaymentController(IPaymentService payments, IStripeCheckoutService stripe) : Controller
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

    /// <summary>Starts a real Stripe Checkout Session and sends the member there to pay.</summary>
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> Checkout(int id)
    {
        var payment = await payments.GetDetailsAsync(id);
        if (payment == null) return NotFound();
        if (payment.Borrowing?.UserId != CurrentUserId() || payment.PaymentMethod != "Card") return Forbid();
        if (payment.Status != "Pending") return RedirectToAction(nameof(Details), new { id });

        if (!stripe.IsConfigured)
        {
            TempData["PaymentMessage"] = "Card payments aren't set up yet. Ask an administrator to configure Stripe.";
            return RedirectToAction(nameof(Details), new { id });
        }

        var successUrl = Url.Action(nameof(StripeReturn), "Payment", new { id }, Request.Scheme)!;
        var cancelUrl = Url.Action(nameof(Details), "Payment", new { id }, Request.Scheme)!;
        var sessionUrl = await stripe.CreateBorrowingPaymentSessionAsync(payment, successUrl, cancelUrl);
        return Redirect(sessionUrl);
    }

    /// <summary>
    /// Where Stripe sends the member back after Checkout. The webhook is the
    /// authoritative way a payment gets completed (it works even if the
    /// member closes the tab), but reconciling here too means the Details
    /// page shows the right status immediately instead of waiting on the
    /// webhook round-trip.
    /// </summary>
    [Authorize(Roles = "Member")]
    public async Task<IActionResult> StripeReturn(int id, string? session_id)
    {
        if (!string.IsNullOrEmpty(session_id))
            await stripe.HandleCompletedSessionAsync(session_id);

        var payment = await payments.GetDetailsAsync(id);
        TempData["PaymentMessage"] = payment?.Status switch
        {
            "Paid" => "Payment succeeded. Your borrowing is now active.",
            "Failed" => "The payment wasn't completed. The book is available again.",
            _ => "We're still confirming your payment with Stripe - refresh in a moment if the status doesn't update."
        };
        return RedirectToAction(nameof(Details), new { id });
    }

    private int CurrentUserId() => int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0;
}
