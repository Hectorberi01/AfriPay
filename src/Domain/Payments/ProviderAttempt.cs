namespace AfriPay.Domain.Payments;

/// <summary>
/// Enregistrement de chaque tentative d'appel provider.
/// Permet de tracer retries, fallbacks et latences pour le monitoring.
/// </summary>
public sealed class ProviderAttempt
{
    public Guid           Id                { get; private set; }
    public Guid           PaymentId         { get; private set; }
    public string         ProviderKey       { get; private set; } = default!;
    public string?        ProviderReference { get; private set; }
    public bool           Success           { get; private set; }
    public string?        ErrorCode         { get; private set; }
    public string?        ErrorMessage      { get; private set; }
    public int?           LatencyMs         { get; private set; }
    public DateTimeOffset AttemptedAt       { get; private set; }
 
    private ProviderAttempt() { }
 
    internal static ProviderAttempt Create(
        Guid   paymentId,
        string providerKey,
        string? providerReference,
        bool   success,
        string? errorCode,
        string? errorMessage,
        int?   latencyMs) => new()
    {
        Id                = Guid.NewGuid(),
        PaymentId         = paymentId,
        ProviderKey       = providerKey,
        ProviderReference = providerReference,
        Success           = success,
        ErrorCode         = errorCode,
        ErrorMessage      = errorMessage,
        LatencyMs         = latencyMs,
        AttemptedAt       = DateTimeOffset.UtcNow,
    };
}