using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record ListRefundsByPaymentQuery(Guid PaymentId, Guid MerchantId)
    : MediatR.IRequest<Result<IReadOnlyList<RefundResponse>>>;