using AfriPay.Application.Analytics.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Analytics.Queries;

public sealed record GetPayoutSummaryQuery(Guid MerchantId, string Currency);

public sealed class GetPayoutSummaryHandler(IUnitOfWork uow)
{
    public async Task<Result<PayoutSummaryDto>> HandleAsync(
        GetPayoutSummaryQuery query, CancellationToken ct = default)
    {
        var payouts = await uow.Payouts.GetByMerchantAsync(
            query.MerchantId, null, 1, 1000, ct);
 
        var currency  = query.Currency.ToUpperInvariant();
        var completed = payouts.Where(p =>
            p.Status   == Domain.Payouts.PayoutStatus.Completed
            && p.Currency == currency);
 
        var pending = payouts.Where(p =>
            p.Status   == Domain.Payouts.PayoutStatus.Pending
            && p.Currency == currency);
 
        return Result<PayoutSummaryDto>.Ok(new PayoutSummaryDto(
            completed.Sum(p => p.Amount),
            completed.Count(),
            pending.Sum(p   => p.Amount),
            currency));
    }
}