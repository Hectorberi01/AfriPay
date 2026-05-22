using System.Globalization;
using AfriPay.API.Contracts.Responses.PayPal;
using AfriPay.Infrastructure.Providers.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace AfriPay.Infrastructure.Providers.Paypal;

/// <summary>
/// Adapter PayPal — implémente IPaymentProvider.
/// Utilise l'API PayPal Orders v2 (REST).
///
/// Flux :
///   1. OAuth2 : obtient un access token (mis en cache 8h)
///   2. POST /v2/checkout/orders → crée un Order CAPTURE
///   3. Retourne le lien d'approbation (redirect_url) pour le payeur
///   4. Le payeur approuve → webhook CHECKOUT.ORDER.APPROVED → capture
/// </summary>
public sealed class PayPalAdapter(
    PayPalClient               client,
    IMemoryCache               cache,
    ILogger<PayPalAdapter>     logger) : IPaymentProvider
{
    public string ProviderKey => "paypal";
 
    private const string TokenCacheKeyPrefix = "paypal_token:";
    
    public async Task<ProviderResult> InitiateAsync(PaymentRequest request, CancellationToken ct = default)
    {
        var isLive = request.IsLive;
        var token  = await GetTokenAsync(isLive, ct);
 
        // Convertir le montant en format décimal PayPal
        // PayPal attend des strings : "12.50" pour EUR, "5000" pour XOF (0 décimales)
        var (value, currency) = FormatAmount(request.Amount, request.Currency);
 
        var referenceId = NormalizeReferenceId(request.IdempotencyKey);
        
        var body = new PayPalOrderRequest(
            Intent: "CAPTURE",
            PurchaseUnits:
            [
                new PayPalPurchaseUnit(
                    Amount:      new PayPalAmount(currency, value),
                    ReferenceId: referenceId,
                    Description: request.Description ?? "AfriPay payment")
            ],
            PaymentSource: request.WebhookUrl is not null
                ? new PayPalPaymentSource(
                    Wallet: new PayPalWallet(
                        ExperienceContext: new PayPalExperienceContext(
                            ReturnUrl: request.WebhookUrl,
                            CancelUrl: request.WebhookUrl)))
                : null
        );
        try
        {
            var order = await client.CreateOrderAsync(body, token, isLive, referenceId, ct);
 
            // Lien d'approbation pour rediriger le payeur
            var approveLink = order.Links?
                .FirstOrDefault(l =>
                    l.Rel.Equals("approve",      StringComparison.OrdinalIgnoreCase) ||
                    l.Rel.Equals("payer-action", StringComparison.OrdinalIgnoreCase))
                ?.Href;
 
            logger.LogInformation(
                "PayPal Order created: id={OrderId} status={Status} env={Env}",
                order.Id, order.Status, isLive ? "live" : "sandbox");
 
            return new ProviderResult
            {
                ProviderKey       = ProviderKey,
                ProviderReference = order.Id,
                IsSuccess         = true,
                RedirectUrl       = approveLink,
            };
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "PayPal CreateOrder failed");
            return ProviderResult.Failure(
                ProviderKey,
                "PROVIDER_HTTP_ERROR",
                $"PayPal error: {ex.Message}",
                retryable: IsRetryable(ex));
        }
    }
 
    public async Task<ProviderResult> GetStatusAsync(string providerReference, bool isLive, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(isLive, ct);
 
        try
        {
            var order = await client.GetOrderAsync(providerReference, token, isLive, ct);
 
            return order.Status switch
            {
                "COMPLETED" or "APPROVED" => ProviderResult.Success(
                    ProviderKey, providerReference),
 
                "VOIDED" or "FAILED" => ProviderResult.Failure(
                    ProviderKey, "PAYMENT_FAILED",
                    $"PayPal order status: {order.Status}", retryable: false),
 
                _ => new ProviderResult
                {
                    ProviderKey       = ProviderKey,
                    ProviderReference = providerReference,
                    IsSuccess         = false,
                },
            };
        }
        catch (HttpRequestException ex)
        {
            return ProviderResult.Failure(
                ProviderKey, "PROVIDER_HTTP_ERROR", ex.Message, retryable: true);
        }
    }
    
    /// <summary>
    /// Capture un Order PayPal approuvé.
    /// Appelé par PayPalWebhookHandler après CHECKOUT.ORDER.APPROVED.
    /// </summary>
    public async Task<ProviderResult> CaptureAsync(string orderId, bool isLive, CancellationToken ct = default)
    {
        try
        {
            var token    = await GetTokenAsync(isLive, ct);
            var captured = await client.CaptureOrderAsync(orderId, token, isLive, ct);
 
            return captured.Status is "COMPLETED"
                ? ProviderResult.Success(ProviderKey, orderId)
                : ProviderResult.Failure(ProviderKey, "CAPTURE_FAILED",
                    $"PayPal capture status: {captured.Status}", retryable: false);
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "PayPal capture failed for orderId={OrderId}", orderId);
            return ProviderResult.Failure(ProviderKey, "PROVIDER_HTTP_ERROR",
                ex.Message, retryable: IsRetryable(ex));
        }
    }
 
    public Task<ProviderResult> CancelAsync(
        string providerReference, bool isLive, CancellationToken ct = default)
        => Task.FromResult(ProviderResult.Failure(
            ProviderKey,
            "CANCEL_NOT_SUPPORTED",
            "PayPal Orders cannot be cancelled via API after creation. " +
            "The payer can cancel on the PayPal approval page.",
            retryable: false));
 
    public async Task<bool> IsAvailableAsync(bool isLive, CancellationToken ct = default)
    {
        try { await GetTokenAsync(isLive, ct); return true; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "PayPal health check failed");
            return false;
        }
    }
 
    // ── Token cache ────────────────────────────────────────────
 
    private async Task<string> GetTokenAsync(bool isLive, CancellationToken ct)
    {
        var cacheKey = $"{TokenCacheKeyPrefix}{(isLive ? "live" : "sandbox")}";
 
        if (cache.TryGetValue(cacheKey, out string? token) && token is not null)
            return token;
 
        token = await client.GetAccessTokenAsync(isLive, ct);
 
        // Token PayPal valide ~8h30 — on met en cache 8h pour la marge
        cache.Set(cacheKey, token, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(8),
        });
 
        return token;
    }
 
    // ── Helpers ────────────────────────────────────────────────
 
    /// <summary>
    /// Convertit le montant en unités minimales vers le format décimal PayPal.
    /// EUR : centimes → "12.50" | XOF : entier → "5000"
    /// </summary>
    private static (string Value, string Currency) FormatAmount(long amount, string currency)
    {
        var normalizedCurrency = currency.ToUpperInvariant();

        return normalizedCurrency switch
        {
            // Devises sans décimales
            "XOF" or "XAF" or "GNF" or "JPY" or "KRW"
                => (amount.ToString(CultureInfo.InvariantCulture), normalizedCurrency),

            // Devises avec 2 décimales
            _ => (
                (amount / 100m).ToString("F2", CultureInfo.InvariantCulture),
                normalizedCurrency
            )
        };
    }
 
    private static string NormalizeReferenceId(string key)
        => Guid.TryParse(key, out var g) ? g.ToString() : Guid.NewGuid().ToString();
 
    private static bool IsRetryable(HttpRequestException ex)
        => ex.StatusCode is
            System.Net.HttpStatusCode.ServiceUnavailable or
            System.Net.HttpStatusCode.GatewayTimeout     or
            System.Net.HttpStatusCode.TooManyRequests;
}