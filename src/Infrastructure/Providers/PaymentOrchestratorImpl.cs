using AfriPay.API.Contracts.Requests;
using AfriPay.API.Contracts.Responses;
using AfriPay.Infrastructure.Providers.Abstractions;
using Polly.CircuitBreaker;
using Polly.Registry;

namespace AfriPay.Infrastructure.Providers;

public interface IPaymentOrchestrator
{
    Task<OrchestratorResult> InitiateAsync(
        string providerKey,
        OrchestratorRequest request,
        CancellationToken ct = default);
}

/// <summary>
/// Implémentation de IPaymentOrchestrator (interface définie dans Application).
/// Combine : factory de provider + Polly (retry + circuit breaker) + fallback automatique.
/// </summary>
public sealed class PaymentOrchestratorImpl(
    IPaymentProviderFactory              factory,
    ResiliencePipelineProvider<string>   pipelineProvider,
    ILogger<PaymentOrchestratorImpl>     logger)
    : IPaymentOrchestrator
{
    public async Task<OrchestratorResult> InitiateAsync(string providerKey, OrchestratorRequest request, CancellationToken  ct = default)
    {
        var provider = factory.Resolve(providerKey, request.PhoneNumber);
        var pipeline = pipelineProvider.GetPipeline(provider.ProviderKey);

        try
        {
            var result = await pipeline.ExecuteAsync(
                async token => await provider.InitiateAsync(
                    MapRequest(request), token),
                ct);

            return ToOrchestratorResult(result);
        }
        catch (BrokenCircuitException)
        {
            return await TryFallbackAsync(request, provider.ProviderKey, ct);
        }
    }

    //Fallback
    private async Task<OrchestratorResult> TryFallbackAsync(OrchestratorRequest request, string failedProviderKey, CancellationToken ct)
    {
        var fallbackKey = GetFallbackProvider(failedProviderKey);

        if (fallbackKey is null)
        {
            logger.LogError("No fallback available for provider {Provider}.", failedProviderKey);

            return new OrchestratorResult(
                failedProviderKey, null, false,
                "PROVIDER_UNAVAILABLE",
                $"Provider '{failedProviderKey}' is unavailable and no fallback is configured.",
                null,
                null);
        }

        logger.LogInformation("Falling back from {Failed} to {Fallback}.", failedProviderKey, fallbackKey);

        var fallback = factory.Resolve(fallbackKey);
        var result   = await fallback.InitiateAsync(MapRequest(request), ct);

        return ToOrchestratorResult(result);
    }

    //Table de fallback
    private static string? GetFallbackProvider(string providerKey) => providerKey switch
    {
        "mtn_momo"     => "moov_money",
        "moov_money"   => "mtn_momo",
        "wave"         => "orange_money",
        "orange_money" => "wave",
        "stripe"       => "paypal",
        "paypal"       => null,
        _              => null,
    };

    //Helpers
    private static PaymentRequest MapRequest(OrchestratorRequest r) =>
        new()
        {
            Amount         = r.Amount,
            Currency       = r.Currency,
            IdempotencyKey = r.IdempotencyKey,
            PhoneNumber    = r.PhoneNumber,
            Email          = r.Email,
            Metadata       = r.Metadata,
            IsLive         = r.IsLive,
        };

    private static OrchestratorResult ToOrchestratorResult(ProviderResult r) =>
        new(
            r.ProviderKey,
            r.ProviderReference,
            r.IsSuccess,
            r.Error?.Code,
            r.Error?.Message,
            r.UssdCode,
            r.RedirectUrl
            );
}