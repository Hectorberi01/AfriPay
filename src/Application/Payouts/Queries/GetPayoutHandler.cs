using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payouts.Dtos;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Payouts.Queries;

public sealed record GetPayoutQuery(Guid PayoutId, Guid MerchantId);
 
public sealed class GetPayoutHandler(IUnitOfWork uow)
{
    public async Task<Result<PayoutDto>> HandleAsync(
        GetPayoutQuery query, CancellationToken ct = default)
    {
        var payout = await uow.Payouts.GetByIdAsync(query.PayoutId, ct);
 
        if (payout is null)
            return Result<PayoutDto>.Fail(
                AppError.NotFound("Payout", query.PayoutId.ToString()));
 
        if (payout.MerchantId != query.MerchantId)
            return Result<PayoutDto>.Fail(AppError.Unauthorized());
 
        return Result<PayoutDto>.Ok(PayoutDto.FromDomain(payout));
    }
}