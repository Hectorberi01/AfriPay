using AfriPay.Infrastructure.Providers.Abstractions;
using Microsoft.Extensions.Caching.Memory;

namespace AfriPay.Infrastructure.Providers.MtnMomo;


public sealed class MtnMomoAdapter(MtnMomoClient client, IMemoryCache cache, ILogger<MtnMomoAdapter> logger) : IPaymentProvider
{
    public string ProviderKey => "mtn_momo";
 
    private const string TokenCacheKey = "mtn_momo:access_token";
 
    public async Task<ProviderResult> InitiateAsync(PaymentRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.PhoneNumber))
            return ProviderResult.Failure(ProviderKey, "MISSING_PHONE", "MTN MoMo requires a phone number.", retryable: false);
 
        var isLive      = request.IsLive;
        
        var token       = await GetTokenAsync(isLive,ct);
        var referenceId = NormalizeReferenceId(request.IdempotencyKey);
 
        var body = new MtnRequestToPayBody(
            amount:       request.Amount,
            currency:     request.Currency,
            externalId:   referenceId,
            payer:        new MtnParty("MSISDN", request.PhoneNumber.TrimStart('+')),
            payerMessage: request.Description ?? "AfriPay payment",
            payeeNote:    request.Metadata.GetValueOrDefault("order_id", referenceId));
 
        try
        {
            await client.RequestToPayAsync(referenceId, body, token ,isLive, ct);
 
            return ProviderResult.Success(
                ProviderKey,
                referenceId,
                ussdCode: $"*880*1*{referenceId[..8]}#");
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex,
                "MTN RequestToPay failed for referenceId={ReferenceId}", referenceId);
 
            return ProviderResult.Failure(
                ProviderKey,
                "PROVIDER_HTTP_ERROR",
                $"MTN MoMo error: {ex.Message}",
                retryable: IsRetryable(ex));
        }
    }
 
    public async Task<ProviderResult> GetStatusAsync(string providerReference,bool isLive, CancellationToken ct = default)
    {
        var token = await GetTokenAsync(isLive,ct);
 
        try
        {
            var status = await client.GetTransactionStatusAsync(providerReference, token,isLive, ct);
 
            return status.status switch
            {
                "SUCCESSFUL" => ProviderResult.Success(
                    ProviderKey,
                    status.financialTransactionId ?? providerReference),
 
                "FAILED" => ProviderResult.Failure(
                    ProviderKey,
                    status.reason ?? "FAILED",
                    MapFailureReason(status.reason),
                    retryable: false),
 
                _ => new ProviderResult
                {
                    ProviderKey       = ProviderKey,
                    ProviderReference = providerReference,
                    IsSuccess         = false,
                    Error             = null,   // encore pending
                },
            };
        }
        catch (HttpRequestException ex)
        {
            return ProviderResult.Failure(
                ProviderKey, "PROVIDER_HTTP_ERROR", ex.Message, retryable: true);
        }
    }
 
    public Task<ProviderResult> CancelAsync(
        string providerReference, bool isLive,CancellationToken ct = default)
        => Task.FromResult(ProviderResult.Failure(
            ProviderKey,
            "CANCEL_NOT_SUPPORTED",
            "MTN MoMo does not support payment cancellation. " +
            "Wait for expiry or initiate a refund after completion.",
            retryable: false));
 
    public async Task<bool> IsAvailableAsync(bool isLive, CancellationToken ct = default)
    {
        try { await GetTokenAsync(isLive,ct); return true; }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "MTN MoMo health check failed");
            return false;
        }
    }
 
    // Token cache
    private async Task<string> GetTokenAsync(bool isLive, CancellationToken ct)
    {
        var cacheKey = $"{TokenCacheKey}:{(isLive ? "live" : "sandbox")}";
        
        if (cache.TryGetValue(cacheKey, out string? token) && token is not null)
            return token;
 
        token = await client.GetAccessTokenAsync(isLive,ct);
        cache.Set(TokenCacheKey, token, new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(3540),
            Size = 1
        });
        return token;
    }
 
    // Helpers
 
    private static string NormalizeReferenceId(string key) =>
        Guid.TryParse(key, out var g) ? g.ToString() : Guid.NewGuid().ToString();
 
    private static bool IsRetryable(HttpRequestException ex) =>
        ex.StatusCode is System.Net.HttpStatusCode.ServiceUnavailable
            or System.Net.HttpStatusCode.GatewayTimeout
            or System.Net.HttpStatusCode.TooManyRequests;
 
    private static string MapFailureReason(string? reason) => reason switch
    {
        "PAYER_NOT_FOUND"       => "Le numéro n'est pas enregistré sur MTN MoMo.",
        "NOT_ENOUGH_FUNDS"      => "Solde insuffisant sur le compte MoMo.",
        "PAYER_LIMIT_REACHED"   => "Plafond de transaction MTN atteint.",
        "APPROVAL_REJECTED"     => "Le payeur a refusé la demande.",
        "EXPIRED"               => "La demande a expiré (30 minutes).",
        _ => $"Échec MTN MoMo : {reason ?? "raison inconnue"}.",
    };
}