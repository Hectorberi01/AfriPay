using AfriPay.Domain.Payments;

namespace AfriPay.Application.Payments.Dtos;

public sealed record PaymentDto(
    Guid            Id,
    string          Status,
    string          ProviderUsed,
    long            Amount,
    string          Currency,
    long?           FeeAmount,
    long?           NetAmount,
    string?         ProviderReference,
    string?         UssdCode,
    Dictionary<string, string> Metadata,
    DateTimeOffset  CreatedAt,
    DateTimeOffset  ExpiresAt,
    DateTimeOffset? CompletedAt
)
{
    public static PaymentDto FromDomain(Payment p) => new(
        p.Id,
        p.Status.ToString().ToLower(),
        p.ProviderUsed ?? p.ProviderKey,
        p.Amount.Amount,
        p.Amount.Currency,
        p.Fee?.Amount,
        p.Net?.Amount,
        p.ProviderReference,
        p.UssdCode,
        p.Metadata,
        p.CreatedAt,
        p.ExpiresAt,
        p.CompletedAt
    );
}
