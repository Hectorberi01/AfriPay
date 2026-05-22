using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Repositories;
using AfriPay.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class WebhookDeliveryRepository(AfriPayDbContextBase db)
    : Repository<WebhookDelivery>(db), IWebhookDeliveryRepository
{
    public async Task<IReadOnlyList<WebhookDelivery>> GetDueForDeliveryAsync(
        int batchSize = 50, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;
        return await Db.WebhookDeliveries
            .Where(w =>
                (w.Status == DeliveryStatus.Pending  && w.CreatedAt    <= now) ||
                (w.Status == DeliveryStatus.Retrying && w.NextRetryAt  <= now))
            .OrderBy(w => w.NextRetryAt ?? w.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
    }
 
    public async Task<PagedResult<WebhookDelivery>> ListByMerchantAsync(
        Guid merchantId, int page, int pageSize, CancellationToken ct = default)
    {
        var total = await Db.WebhookDeliveries
            .CountAsync(w => w.MerchantId == merchantId, ct);
 
        var items = await Db.WebhookDeliveries
            .Where(w => w.MerchantId == merchantId)
            .OrderByDescending(w => w.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
 
        return new PagedResult<WebhookDelivery>(items, total, page, pageSize, HasMore:page * pageSize < items.Count);
    }
 
    public async Task<WebhookDelivery?> GetWithAttemptsAsync(
        Guid deliveryId, CancellationToken ct = default)
        => await Db.WebhookDeliveries
            .Include(w => w.Attempts)
            .FirstOrDefaultAsync(w => w.Id == deliveryId, ct);
 
    public async Task<int> CountDeadLetterAsync(
        Guid merchantId, CancellationToken ct = default)
        => await Db.WebhookDeliveries
            .CountAsync(w => w.MerchantId == merchantId
                             && w.Status     == DeliveryStatus.DeadLetter, ct);
}