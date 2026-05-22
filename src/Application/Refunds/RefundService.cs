using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Refunds.Commands;
using AfriPay.Application.Refunds.Queries;
using AfriPay.Domain.Refunds;

namespace AfriPay.Application.Refunds;


public sealed record RefundDto(
    string          RefundId,
    string          PaymentId,
    string          Status,
    string          Reason,
    bool            IsPartial,
    long            Amount,
    string          Currency,
    string?         ProviderReference,
    string?         Notes,
    DateTimeOffset  CreatedAt,
    DateTimeOffset  EstimatedArrival,
    DateTimeOffset? CompletedAt,
    DateTimeOffset? FailedAt)
{
    public static RefundDto FromDomain(Refund r) => new(
        r.Id.ToString(),
        r.PaymentId.ToString(),
        r.Status.ToString().ToLower(),
        r.Reason.ToString().ToLower(),
        r.IsPartial,
        r.Amount.Amount,
        r.Amount.Currency,
        r.ProviderReference,
        r.Notes,
        r.CreatedAt,
        r.EstimatedArrival,
        r.CompletedAt,
        r.FailedAt);
}

public interface IRefundService
{
    Task<Result<RefundDto>> CreateAsync(CreateRefundCommand dto, CancellationToken ct = default);
    Task<Result<RefundDto>> GetByIdAsync(GetRefundQuery refundQuery, CancellationToken ct = default);
    Task<Result<IReadOnlyList<RefundDto>>> ListByPaymentAsync(ListRefundsByPaymentQuery listRefundsByPaymentQuery, CancellationToken ct = default);
}



public sealed class RefundService(
    CreateRefundHandler createRefundHandler,
    GetRefundHandler getRefundHandler,
    ListRefundsByPaymentHandler listRefundsByPaymentHandler
    ) : IRefundService
{

    public async Task<Result<RefundDto>> CreateAsync(CreateRefundCommand dto, CancellationToken ct = default)
        => await createRefundHandler.HandleAsync(dto, ct);
    
    public async Task<Result<RefundDto>> GetByIdAsync(GetRefundQuery refundQuery, CancellationToken ct = default)
        => await getRefundHandler.HandleAsync(refundQuery, ct);

    public async Task<Result<IReadOnlyList<RefundDto>>> ListByPaymentAsync(
        ListRefundsByPaymentQuery listRefundsByPaymentQuery, CancellationToken ct = default)
        => await listRefundsByPaymentHandler.HandleAsync(listRefundsByPaymentQuery, ct);
}