namespace AfriPay.Infrastructure.Providers.Abstractions;

/// <summary>
/// Requête unifiée envoyée à tous les adapters providers.
/// Traduite depuis le domaine Payment dans l'orchestrateur.
/// </summary>
public sealed record PaymentRequest
{
    public required long                        Amount         { get; init; }
    public required string                      Currency       { get; init; }
    public required string                      IdempotencyKey { get; init; }
    public string?                              PhoneNumber    { get; init; }
    public string?                              Email          { get; init; }
    public string?                              Description    { get; init; }
    public Dictionary<string, string>           Metadata       { get; init; } = [];
    public string?                              WebhookUrl     { get; init; }
    public bool                                 IsLive         { get; init; }
}
 
/// <summary>
/// Résultat normalisé retourné par tous les adapters.
/// L'orchestrateur mappe ce résultat vers le domaine Payment.
/// </summary>
public sealed record ProviderResult
{
    public required string  ProviderKey       { get; init; }
    public string?          ProviderReference { get; init; }
    public required bool    IsSuccess         { get; init; }
    public string?          UssdCode          { get; init; }
    public string?          RedirectUrl       { get; init; }
    public ProviderError?   Error             { get; init; }
 
    public static ProviderResult Success(string providerKey, string? reference, string? ussdCode = null) =>
        new()
        {
            ProviderKey       = providerKey,
            ProviderReference = reference,
            IsSuccess         = true,
            UssdCode          = ussdCode,
        };
 
    public static ProviderResult Failure(string providerKey, string errorCode, string message, bool retryable = false) =>
        new()
        {
            ProviderKey = providerKey,
            IsSuccess   = false,
            Error       = new ProviderError(errorCode, message, retryable),
        };
}

public sealed record ProviderError(
    string Code,
    string Message,
    bool   IsRetryable
);