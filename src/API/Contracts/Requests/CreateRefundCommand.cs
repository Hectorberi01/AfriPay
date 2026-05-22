using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record CreateRefundCommand(
    Guid PaymentId,
    Guid MerchantId,
    string IdempotencyKey,
    long? Amount,
    string Reason,
    string? Notes)
    : MediatR.IRequest<Result<RefundResponse>>;