using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Dtos;
using AfriPay.Application.Payments.Queries;
using MediatR;

namespace AfriPay.Application.Payments.Dtos;

public sealed record InitiatePaymentDto(
    Guid                        MerchantId,
    string                      IdempotencyKey,
    long                        Amount,
    string                      Currency,
    string                      ProviderKey,
    string?                     PhoneNumber,
    string?                     Email,
    Dictionary<string, string>? Metadata,
    string?                     WebhookUrl,
    bool                        IsLive
) : IRequest<Result<PaymentDto>>;