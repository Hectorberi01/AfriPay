using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Queries;
using AfriPay.Domain.Refunds;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class RefundRepository(AfriPayDbContextBase db)
    : Repository<Refund>(db), IRefundRepository
{
    public async Task<IReadOnlyList<Refund>> GetByPaymentIdAsync(
        Guid paymentId, CancellationToken ct = default)
        => await Db.Refunds
            .Where(r => r.PaymentId == paymentId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(ct);
 
    public async Task<PagedResult<Refund>> ListByMerchantAsync(
        Guid merchantId, RefundQueryFilter filter,
        int page, int pageSize, CancellationToken ct = default)
    {
        var query = Db.Refunds
            .Where(r => r.MerchantId == merchantId)
            .AsQueryable();
 
        if (filter.Status is not null)
            query = query.Where(r => r.Status.ToString() == filter.Status);
        if (filter.From   is not null)
            query = query.Where(r => r.CreatedAt >= filter.From);
        if (filter.To     is not null)
            query = query.Where(r => r.CreatedAt <= filter.To);
 
        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
 
        return new PagedResult<Refund>(
            items, 
            total,
            page, 
            pageSize,
            HasMore:page * pageSize < items.Count
            );
    }
 
    public async Task<IReadOnlyList<Refund>> GetPendingAndProcessingAsync(
        int batchSize = 100, CancellationToken ct = default)
        => await Db.Refunds
            .Where(r => r.Status == RefundStatus.Pending
                     || r.Status == RefundStatus.Processing)
            .OrderBy(r => r.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
 
    public async Task<long> GetTotalRefundedAmountAsync(
        Guid paymentId, CancellationToken ct = default)
        => await Db.Refunds
            .Where(r => r.PaymentId == paymentId && r.Status    == RefundStatus.Completed)
            .SumAsync(r => r.Amount.Amount, ct);
}