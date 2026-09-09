using Lib_System.Models;

namespace Lib_System.Services.Interfaces;

/// <summary>
/// Wraps Stripe Checkout for the two card-payment flows in the app:
/// paying to start a borrowing, and paying off a fine. Actually completing
/// a payment/fine (updating our own database) is still done through
/// IPaymentWorkflowService / IFinePaymentService - this service only talks
/// to Stripe and translates a Checkout Session outcome into a call on those.
/// </summary>
public interface IStripeCheckoutService
{
    /// <summary>True once a Stripe secret key has been configured (user-secrets/env). Controllers use
    /// this to fail gracefully with a friendly message instead of throwing when Stripe isn't set up yet.</summary>
    bool IsConfigured { get; }

    Task<string> CreateBorrowingPaymentSessionAsync(Payment payment, string successUrl, string cancelUrl);

    Task<string> CreateFinePaymentSessionAsync(Fine fine, string successUrl, string cancelUrl);

    /// <summary>
    /// Re-fetches the given Checkout Session from Stripe (never trusts a
    /// webhook payload's field values directly) and, if it has reached a
    /// final state (paid or expired), completes the matching payment/fine.
    /// Safe to call more than once for the same session - completion on our
    /// side is already idempotent.
    /// </summary>
    Task HandleCompletedSessionAsync(string sessionId);
}
