using AfriPay.API.Contracts.Responses;
using AfriPay.API.Controllers;
using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record RetryWebhookDeliveryCommand(Guid DeliveryId, Guid MerchantId)
    : MediatR.IRequest<Result<WebhookDeliveryResponse>>;