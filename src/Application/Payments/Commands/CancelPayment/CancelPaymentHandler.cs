using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Domain.Repositories;
using MediatR;

namespace AfriPay.Application.Payments.Commands.CancelPayment;


public sealed class CancelPaymentHandler(IUnitOfWork uow) : IRequestHandler<CancelPaymentDto, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(CancelPaymentDto cmd, CancellationToken cancellationToken)
    {
        var payment = await uow.Payments.GetByIdAsync(cmd.PaymentId, cancellationToken);
 
        if (payment is null)
            return Result<PaymentDto>.Fail(
                AppError.NotFound("Payment", cmd.PaymentId.ToString()));
 
        if (payment.MerchantId != cmd.MerchantId)
            return Result<PaymentDto>.Fail(
                AppError.Unauthorized());
 
        if (payment.Status != AfriPay.Domain.Payments.PaymentStatus.Pending)
            return Result<PaymentDto>.Fail(
                AppError.Conflict("Only pending payments can be cancelled."));
 
        payment.MarkCancelled();
        uow.Payments.Update(payment);
        await uow.SaveChangesAsync(cancellationToken);
 
        return Result<PaymentDto>.Ok(PaymentDto.FromDomain(payment));
    }
}