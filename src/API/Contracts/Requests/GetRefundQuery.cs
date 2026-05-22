using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record GetRefundQuery(Guid RefundId, Guid MerchantId)
    : MediatR.IRequest<Result<RefundResponse>>;