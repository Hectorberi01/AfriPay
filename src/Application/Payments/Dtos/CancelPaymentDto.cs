using AfriPay.Application.Common.Errors;
using MediatR;

namespace AfriPay.Application.Payments.Dtos;
public sealed record CancelPaymentDto(Guid PaymentId, Guid MerchantId) : IRequest<Result<PaymentDto>>;
