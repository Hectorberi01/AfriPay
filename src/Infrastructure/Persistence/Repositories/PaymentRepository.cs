using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payments.Queries;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class PaymentRepository(AfriPayDbContextBase db) : Repository<Payment>(db), IPaymentRepository
{
    public async Task<Payment?> GetByIdempotencyKeyAsync(Guid merchantId, string key, CancellationToken ct = default)
        => await Db.Payments.Include(p => p.Transitions).Include(p => p.Attempts)
            .FirstOrDefaultAsync(p => p.MerchantId == merchantId && p.IdempotencyKey == key, ct);
 
    public async Task<Payment?> GetByProviderReferenceAsync(string ref_, CancellationToken ct = default)
        => await Db.Payments.FirstOrDefaultAsync(p => p.ProviderReference == ref_, ct);
 
    public async Task<PagedResult<Payment>> ListAsync(Guid merchantId, PaymentQueryFilter f, int page, int size, CancellationToken ct = default)
    {
        var q = Db.Payments.Where(p => p.MerchantId == merchantId).AsQueryable();
        if (f.Status      != null) q = q.Where(p => p.Status.ToString()  == f.Status);
        if (f.ProviderKey != null) q = q.Where(p => p.ProviderUsed       == f.ProviderKey);
        if (f.From        != null) q = q.Where(p => p.CreatedAt          >= f.From);
        if (f.To          != null) q = q.Where(p => p.CreatedAt          <= f.To);
        
        var total = await q.CountAsync(ct);
        var items = await q.OrderByDescending(p => p.CreatedAt)
            .Skip((page-1)*size)
            .Take(size)
            .ToListAsync(ct);
        
       var result = new PagedResult<Payment>(
           items, 
           total,
           page, 
           size,
           HasMore:page * size < items.Count);
        return result;
    }

    public async Task<IReadOnlyList<Payment>> GetExpiredPendingAsync(int batchSize = 200, CancellationToken ct = default)
    {
        var list = await Db.Payments
            .Where(p => p.Status != PaymentStatus.Pending)
            .OrderByDescending(p => p.CreatedAt)
            .Take(batchSize)
            .ToListAsync(ct);
        return list;
    }
 
    public async Task<IReadOnlyList<Payment>> GetExpiredAsync(CancellationToken ct = default)
        => await Db.Payments
            .Where(p => p.Status == PaymentStatus.Pending && p.ExpiresAt < DateTimeOffset.UtcNow)
            .ToListAsync(ct);
    
    public async Task<IReadOnlyList<Payment>> GetSettlementPendingAsync(
        DateTimeOffset cutoff, CancellationToken ct = default)
        => await Db.Payments
            .Where(p => p.Status       == PaymentStatus.Completed
                        && p.CompletedAt  != null
                        && p.CompletedAt  <= cutoff)
            .OrderBy(p => p.CompletedAt)
            .Take(200)
            .ToListAsync(ct);

    public async Task<Payment?> GetWithFullHistoryAsync(Guid paymentId, CancellationToken ct = default)
    {
        var payment = await Db.Payments
            .Where(p => p.Id == paymentId)
            .Include(p => p.Attempts)
            .FirstOrDefaultAsync(ct);
        return payment;
    }
 
    public async Task<PaymentStats> GetStatsAsync(Guid merchantId, DateTimeOffset from, DateTimeOffset to, CancellationToken ct = default)
    {
        
        var d = await Db.Payments
            .Where(p => p.MerchantId == merchantId && p.CreatedAt >= from && p.CreatedAt <= to && p.Status != PaymentStatus.Pending)
            .GroupBy(_ => 1)
            .Select(g =>
                new
                {
                    Vol = g.Sum(p => (long?)p.Amount.Amount)??0L, 
                    Count = g.Count(),
                    Completed = g.Count(p => p.Status == PaymentStatus.Completed), 
                    Fees = g.Sum(p => (long?)p.Fee!.Amount)??0L
                })
            .FirstOrDefaultAsync(ct);
        if (d is null) 
            return new PaymentStats(
                0, 
                0, 
                0, 
                0, 
                0, 
                0,
                0);
        
        return new PaymentStats(
            d.Vol, 
            d.Count, 
            d.Count > 0 ? (long)Math.Round(d.Completed * 100.0 / d.Count, 2) : 0L, 
            (int)d.Fees,
            0,
            0,
            0);
    }
}