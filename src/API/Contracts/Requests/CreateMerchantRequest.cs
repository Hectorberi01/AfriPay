namespace AfriPay.API.Contracts.Requests;

/// <summary>
/// Requête POST /v1/merchants (onboarding)
/// </summary>
public sealed record CreateMerchantRequest
{
    public string BusinessName { get; init; } = default!;
    public string Email        { get; init; } = default!;
 
    /// <summary>Code pays ISO 3166-1 alpha-2 (FR, BJ, CI…).</summary>
    public string Country      { get; init; } = default!;
}