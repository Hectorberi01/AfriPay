
using AfriPay.Infrastructure.Providers.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AfriPay.Infrastructure.Providers.Stripe;

// ─── Config ───────────────────────────────────────────────────

public sealed class StripeConfig
{
    public const string SectionName = "Providers:Stripe";

    public required string SecretKey       { get; init; }
    public required string WebhookSecret   { get; init; }
    public int             TimeoutSeconds  { get; init; } = 30;
}

// ─── Adapter ──────────────────────────────────────────────────

/// <summary>
/// Adapter Stripe — implémente IPaymentProvider.
/// Utilise le SDK Stripe.net pour les paiements carte et SEPA.
/// </summary>
public sealed class StripeAdapter(
    HttpClient                  http,
    IOptions<StripeConfig>      options,
    ILogger<StripeAdapter>      logger) : IPaymentProvider
{
    private readonly StripeConfig _config = options.Value;

    public string ProviderKey => "stripe";

    public Task<ProviderResult> InitiateAsync(
        PaymentRequest request, CancellationToken ct = default)
    {
        // TODO : Stripe PaymentIntent creation via Stripe.net SDK
        logger.LogInformation(
            "Stripe InitiateAsync — amount={Amount} {Currency}",
            request.Amount, request.Currency);

        throw new NotImplementedException("StripeAdapter.InitiateAsync — Phase 1.");
    }

    public Task<ProviderResult> GetStatusAsync(
        string providerReference,bool isLive, CancellationToken ct = default)
    {
        throw new NotImplementedException("StripeAdapter.GetStatusAsync — Phase 1.");
    }

    public Task<ProviderResult> CancelAsync(
        string providerReference,bool isLive, CancellationToken ct = default)
    {
        throw new NotImplementedException("StripeAdapter.CancelAsync — Phase 1.");
    }

    public async Task<bool> IsAvailableAsync(bool isLive,CancellationToken ct = default)
    {
        try
        {
            // Ping léger — vérifie que l'API Stripe répond
            var response = await http.GetAsync("v1/balance", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}