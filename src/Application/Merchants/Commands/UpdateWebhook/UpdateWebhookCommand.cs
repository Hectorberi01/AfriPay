using AfriPay.API.Contracts.Responses;
using AfriPay.Application.Common.Errors;
using MediatR;

namespace AfriPay.Application.Merchants.Commands.UpdateWebhook;

public sealed record UpdateWebhookCommand(
    Guid    MerchantId,
    string  Url,
    string? Secret)
    : IRequest<Result<MerchantResponse>>;