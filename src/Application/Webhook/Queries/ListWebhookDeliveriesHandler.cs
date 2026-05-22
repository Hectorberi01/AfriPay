using AfriPay.API.Contracts.Requests;
using AfriPay.Application.Common.Errors;
using AfriPay.Application.Webhook.Dtos;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Webhook.Queries;

//public sealed record ListWebhookDeliveriesQuery(Guid MerchantId, int Page, int PageSize);
public sealed class ListWebhookDeliveriesHandler(IUnitOfWork uow)
{
    public async Task<Result<PagedResult<WebhookDeliveryDto>>> HandleAsync(
        ListWebhookDeliveriesQuery query, CancellationToken ct = default)
    {
        var paged = await uow.WebhookDeliveries.ListByMerchantAsync(
            query.MerchantId,
            Math.Clamp(query.Page,     1, int.MaxValue),
            Math.Clamp(query.PageSize, 1, 100),
            ct);
 
        var dtos = paged.Items.Select(WebhookDeliveryDto.FromDomain).ToList();
 
        return Result<PagedResult<WebhookDeliveryDto>>.Ok(new PagedResult<WebhookDeliveryDto>(
            dtos, 
            paged.TotalCount,
            paged.Page,
            paged.PageSize,
            HasMore: paged.TotalCount >= paged.TotalCount
            ));
    }
}
