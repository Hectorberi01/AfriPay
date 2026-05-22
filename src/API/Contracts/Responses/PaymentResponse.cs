using AfriPay.Application.Payments.Dtos;
using AfriPay.Application.Payments.Queries;

namespace AfriPay.API.Contracts.Responses;

/// <summary>
/// Réponse unifiée pour tous les endpoints paiement.
/// </summary>
public sealed record PaymentResponse
{
    public string          PaymentId         { get; init; } = default!;
    public string          Status            { get; init; } = default!;
    public string?         ProviderUsed      { get; init; }
    public string?         ProviderReference { get; init; }
 
    // Montants
    public long            Amount            { get; init; }
    public string          Currency          { get; init; } = default!;
    public long?           FeeAmount         { get; init; }
    public long?           NetAmount         { get; init; }
 
    // Actions client
    public string?         UssdCode          { get; init; }
    public string?         RedirectUrl       { get; init; }
 
    // Metadata
    public Dictionary<string, string> Metadata { get; init; } = [];
 
    // Timestamps
    public DateTimeOffset  CreatedAt         { get; init; }
    public DateTimeOffset  ExpiresAt         { get; init; }
    public DateTimeOffset? CompletedAt       { get; init; }
 
    /// <summary>Mappe depuis le DTO Application vers le contrat API.</summary>
    public static PaymentResponse From(PaymentDto dto) => new()
    {
        PaymentId         = dto.Id.ToString(),
        Status            = dto.Status,
        ProviderUsed      = dto.ProviderUsed,
        ProviderReference = dto.ProviderReference,
        Amount            = dto.Amount,
        Currency          = dto.Currency,
        FeeAmount         = dto.FeeAmount,
        NetAmount         = dto.NetAmount,
        UssdCode          = dto.UssdCode,
        Metadata          = dto.Metadata,
        CreatedAt         = dto.CreatedAt,
        ExpiresAt         = dto.ExpiresAt,
        CompletedAt       = dto.CompletedAt,
    };
}