using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Refunds.Queries;


public sealed class ListRefundsByPaymentHandler(IUnitOfWork uow)
{
    public async Task<Result<IReadOnlyList<RefundDto>>> HandleAsync(
        ListRefundsByPaymentQuery query, CancellationToken ct = default)
    {
        // Vérifier que le paiement appartient au marchand
        var payment = await uow.Payments.GetByIdAsync(query.PaymentId, ct);
 
        if (payment is null)
            return Result<IReadOnlyList<RefundDto>>.Fail(
                AppError.NotFound("Payment", query.PaymentId.ToString()));
 
        if (payment.MerchantId != query.MerchantId)
            return Result<IReadOnlyList<RefundDto>>.Fail(AppError.Unauthorized());
 
        var refunds = await uow.Refunds.GetByPaymentIdAsync(query.PaymentId, ct);
        var dtos    = (IReadOnlyList<RefundDto>)refunds.Select(RefundDto.FromDomain).ToList();
 
        return Result<IReadOnlyList<RefundDto>>.Ok(dtos);
    }
}