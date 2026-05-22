using AfriPay.API.Contracts.Responses;
using AfriPay.API.Controllers;
using AfriPay.Application.Common.Errors;

namespace AfriPay.API.Contracts.Requests;

public record ListWebhookDeliveriesQuery(Guid MerchantId, int Page, int PageSize)
    : MediatR.IRequest<Result<PagedResponse<WebhookDeliveryResponse>>>;