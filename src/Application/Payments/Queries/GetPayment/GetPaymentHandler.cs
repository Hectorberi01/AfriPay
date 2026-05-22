using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Domain.Repositories;
using MediatR;

namespace AfriPay.Application.Payments.Queries.GetPayment;

public sealed class GetPaymentHandler(IUnitOfWork uow) : IRequestHandler<GetPaymentQuery, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(
        GetPaymentQuery request, CancellationToken ct)
    {
        var payment = await uow.Payments.GetByIdAsync(request.PaymentId, ct);
 
        if (payment is null)
            return Result<PaymentDto>.Fail(
                AppError.NotFound("Payment", request.PaymentId.ToString()));
 
        if (payment.MerchantId != request.MerchantId)
            return Result<PaymentDto>.Fail(AppError.Unauthorized());
 
        return Result<PaymentDto>.Ok(PaymentDto.FromDomain(payment));
    }
}