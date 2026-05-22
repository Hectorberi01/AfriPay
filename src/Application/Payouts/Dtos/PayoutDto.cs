using AfriPay.Domain.Payouts;

namespace AfriPay.Application.Payouts.Dtos;

public sealed record PayoutDto(
    string          PayoutId,
    string          MerchantId,
    string          Status,
    string          Method,
    long            Amount,
    string          Currency,
    string?         ProviderReference,
    string?         FailureReason,
    string?         Notes,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? FailedAt)
{
    public static PayoutDto FromDomain(Payout p) => new(
        p.Id.ToString(),
        p.MerchantId.ToString(),
        p.Status.ToString().ToLower(),
        p.Method.ToString().ToLower(),
        p.Amount,
        p.Currency,
        p.ProviderReference,
        p.FailureReason,
        p.Notes,
        p.CreatedAt,
        p.CompletedAt,
        p.FailedAt);
}