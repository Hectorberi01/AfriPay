using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Refunds.Queries;


public sealed class GetRefundHandler(IUnitOfWork uow)
{
    public async Task<Result<RefundDto>> HandleAsync(
        GetRefundQuery query, CancellationToken ct = default)
    {
        var refund = await uow.Refunds.GetByIdAsync(query.RefundId, ct);
 
        if (refund is null)
            return Result<RefundDto>.Fail(
                AppError.NotFound("Refund", query.RefundId.ToString()));
 
        if (refund.MerchantId != query.MerchantId)
            return Result<RefundDto>.Fail(AppError.Unauthorized());
 
        return Result<RefundDto>.Ok(RefundDto.FromDomain(refund));
    }
}