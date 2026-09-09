using System.Globalization;
using Lib_System.Models;
using Lib_System.Services.Interfaces;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Lib_System.Services;

public class StripeCheckoutService : IStripeCheckoutService
{
    private const string Currency = "usd";

    private readonly SessionService? _sessions;
    private readonly IPaymentWorkflowService _paymentWorkflow;
    private readonly IFinePaymentService _finePayments;
    private readonly ILogger<StripeCheckoutService> _logger;

    public StripeCheckoutService(
        IOptions<StripeSettings> options,
        IPaymentWorkflowService paymentWorkflow,
        IFinePaymentService finePayments,
        ILogger<StripeCheckoutService> logger)
    {
        var settings = options.Value;
        IsConfigured = !string.IsNullOrWhiteSpace(settings.SecretKey);
        // A dedicated StripeClient per settings instance (rather than the
        // static StripeConfiguration.ApiKey) keeps this testable and avoids
        // global mutable state shared across requests.
        _sessions = IsConfigured ? new SessionService(new StripeClient(settings.SecretKey)) : null;
        _paymentWorkflow = paymentWorkflow;
        _finePayments = finePayments;
        _logger = logger;
    }

    public bool IsConfigured { get; }

    public async Task<string> CreateBorrowingPaymentSessionAsync(Payment payment, string successUrl, string cancelUrl)
    {
        if (_sessions == null)
            throw new InvalidOperationException("Stripe is not configured. Set Stripe:SecretKey (e.g. via user-secrets) first.");
        var sessions = _sessions;

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currency,
                        UnitAmount = ToMinorUnits(payment.Amount),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Library borrowing - {payment.Borrowing?.Book?.Title ?? "Book"}",
                            Description = "Reservation payment to start a book borrowing."
                        }
                    }
                }
            ],
            SuccessUrl = WithSessionIdPlaceholder(successUrl),
            CancelUrl = cancelUrl,
            ClientReferenceId = payment.PaymentId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["kind"] = "borrowing-payment",
                ["paymentId"] = payment.PaymentId.ToString(),
                ["memberId"] = payment.Borrowing!.UserId.ToString()
            }
            // No ExpiresAt: our own reservation expiry (checked inside
            // IPaymentWorkflowService.CompleteAsync) is already the source
            // of truth, so a late Stripe success is still rejected there.
        };

        var session = await sessions.CreateAsync(options);
        return session.Url;
    }

    public async Task<string> CreateFinePaymentSessionAsync(Fine fine, string successUrl, string cancelUrl)
    {
        if (_sessions == null)
            throw new InvalidOperationException("Stripe is not configured. Set Stripe:SecretKey (e.g. via user-secrets) first.");
        var sessions = _sessions;

        var amount = fine.PaymentAmount ?? fine.Amount;
        var options = new SessionCreateOptions
        {
            Mode = "payment",
            PaymentMethodTypes = ["card"],
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = Currency,
                        UnitAmount = ToMinorUnits(amount),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Library fine - {fine.Borrowing?.Book?.Title ?? "Book"}",
                            Description = fine.Reason
                        }
                    }
                }
            ],
            SuccessUrl = WithSessionIdPlaceholder(successUrl),
            CancelUrl = cancelUrl,
            ClientReferenceId = fine.FineId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["kind"] = "fine-payment",
                ["fineId"] = fine.FineId.ToString(),
                ["memberId"] = fine.Borrowing!.UserId.ToString(),
                ["attemptId"] = fine.PaymentAttemptId?.ToString() ?? string.Empty,
                ["amount"] = amount.ToString(CultureInfo.InvariantCulture)
            }
        };

        var session = await sessions.CreateAsync(options);
        return session.Url;
    }

    public async Task HandleCompletedSessionAsync(string sessionId)
    {
        if (_sessions == null || string.IsNullOrWhiteSpace(sessionId)) return;

        Session session;
        try
        {
            session = await _sessions.GetAsync(sessionId);
        }
        catch (StripeException ex)
        {
            _logger.LogWarning(ex, "Could not retrieve Stripe Checkout Session {SessionId}.", sessionId);
            return;
        }

        // "paid" is the only outcome that should ever settle a payment/fine.
        // Anything else that is still open (payment_status "unpaid" and the
        // session not yet expired) is left alone - the customer may still be
        // completing the form.
        var success = session.PaymentStatus == "paid";
        var final = success || session.Status == "expired";
        if (!final) return;

        if (!session.Metadata.TryGetValue("kind", out var kind))
        {
            _logger.LogWarning("Stripe Checkout Session {SessionId} has no 'kind' metadata.", sessionId);
            return;
        }

        switch (kind)
        {
            case "borrowing-payment":
                var paymentId = int.Parse(session.Metadata["paymentId"]);
                var payerMemberId = int.Parse(session.Metadata["memberId"]);
                await _paymentWorkflow.CompleteAsync(paymentId, "Card", payerMemberId, success);
                break;

            case "fine-payment":
                var fineId = int.Parse(session.Metadata["fineId"]);
                var fineMemberId = int.Parse(session.Metadata["memberId"]);
                var attemptId = Guid.Parse(session.Metadata["attemptId"]);
                var amount = decimal.Parse(session.Metadata["amount"], CultureInfo.InvariantCulture);
                await _finePayments.CompleteAsync(fineId, fineMemberId, "Card", attemptId, amount, success);
                break;

            default:
                _logger.LogWarning("Stripe Checkout Session {SessionId} has unrecognized kind '{Kind}'.", sessionId, kind);
                break;
        }
    }

    private static string WithSessionIdPlaceholder(string url) =>
        url + (url.Contains('?') ? "&" : "?") + "session_id={CHECKOUT_SESSION_ID}";

    /// <summary>USD has 2 decimal places, so Stripe's smallest unit is cents (amount * 100).</summary>
    private static long ToMinorUnits(decimal amount) => (long)Math.Round(amount * 100m, MidpointRounding.AwayFromZero);
}
