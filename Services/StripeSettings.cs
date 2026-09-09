namespace Lib_System.Services;

/// <summary>
/// Bound from configuration section "Stripe". Keep real keys out of
/// appsettings.json - use `dotnet user-secrets` locally and environment
/// variables / a secret manager in any deployed environment. See
/// HANDOFF_STATUS.txt for the exact commands.
/// </summary>
public class StripeSettings
{
    public string PublishableKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
}
