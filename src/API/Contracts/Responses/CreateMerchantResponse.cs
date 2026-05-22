namespace AfriPay.API.Contracts.Responses;

/// <summary>
/// Réponse de création d'un marchand.
/// Inclut les clés API en clair UNE SEULE FOIS — jamais renvoyées ensuite.
/// </summary>
public sealed record CreateMerchantResponse
{
    public string MerchantId     { get; init; } = default!;
    public string LiveApiKey     { get; init; } = default!;  // afp_live_sk_...
    public string SandboxApiKey  { get; init; } = default!;  // afp_test_sk_...
    public string Warning        { get; init; } =
        "Store these API keys securely. They will not be shown again.";
}
