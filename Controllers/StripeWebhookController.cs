using Lib_System.Services.Interfaces;
using Lib_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Lib_System.Controllers;

/// <summary>
/// Receives Stripe webhook events at /stripe/webhook. This is the one
/// deliberate exception to "controllers serve Razor views, not APIs" -
/// Stripe itself is the caller here, not a browser, so there is no view.
///
/// For local development, forward events with the Stripe CLI:
///   stripe listen --forward-to https://localhost:&lt;port&gt;/stripe/webhook
/// That command prints a whsec_... value to put in Stripe:WebhookSecret.
/// </summary>
[ApiController]
[Route("stripe/webhook")]
[AllowAnonymous]
public class StripeWebhookController(
    IStripeCheckoutService checkout,
    IOptions<StripeSettings> options,
    ILogger<StripeWebhookController> logger) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Handle()
    {
        var webhookSecret = options.Value.WebhookSecret;
        if (string.IsNullOrWhiteSpace(webhookSecret))
        {
            logger.LogWarning("Received a Stripe webhook but Stripe:WebhookSecret is not configured.");
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }

        var json = await new StreamReader(Request.Body).ReadToEndAsync();

        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(json, Request.Headers["Stripe-Signature"], webhookSecret);
        }
        catch (StripeException ex)
        {
            logger.LogWarning(ex, "Rejected a Stripe webhook with an invalid signature.");
            return BadRequest();
        }

        // checkout.session.completed fires on a successful payment.
        // checkout.session.expired fires when the customer never pays -
        // handling both lets Stripe (not just our own reservation-expiry
        // sweep) be the source of truth for failed/abandoned checkouts.
        if (stripeEvent.Type is "checkout.session.completed" or "checkout.session.expired")
        {
            if (stripeEvent.Data.Object is Session session)
            {
                await checkout.HandleCompletedSessionAsync(session.Id);
            }
        }

        return Ok();
    }
}
