using AfriPay.Domain.Disputes;

namespace AfriPay.Application.Disputes.Dtos;

public sealed record DisputeDto(
    string          DisputeId,
    string          PaymentId,
    string          Status,
    string          Reason,
    long            Amount,
    string          Currency,
    long            ChargebackFee,
    long            TotalChargebackAmount,
    string?         ProviderReference,
    string?         CustomerNote,
    string?         MerchantNote,
    string?         ResolutionNote,
    bool            IsOverdue,
    DateTimeOffset  RespondBy,
    DateTimeOffset  CreatedAt,
    DateTimeOffset? ResolvedAt,
    IReadOnlyList<EvidenceDto> Evidence)
{
    public static DisputeDto FromDomain(Dispute d) => new(
        d.Id.ToString(), d.PaymentId.ToString(),
        d.Status.ToString().ToLower(), d.Reason.ToString().ToLower(),
        d.Amount, d.Currency, d.ChargebackFee, d.TotalChargebackAmount,
        d.ProviderReference, d.CustomerNote, d.MerchantNote, d.ResolutionNote,
        d.IsOverdue, d.RespondBy, d.CreatedAt, d.ResolvedAt,
        d.Evidence.Select(EvidenceDto.FromDomain).ToList());
}
