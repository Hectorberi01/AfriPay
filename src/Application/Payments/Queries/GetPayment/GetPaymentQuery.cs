using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Dtos;
using MediatR;

namespace AfriPay.Application.Payments.Queries.GetPayment;

public sealed record GetPaymentQuery(Guid PaymentId, Guid MerchantId) : IRequest<Result<PaymentDto>>;