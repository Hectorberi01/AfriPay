using AfriPay.Domain.Merchants;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class MerchantRepository(AfriPayDbContextBase db) : Repository<Merchant>(db), IMerchantRepository
{
    public async Task<Merchant?> GetByApiKeyHashAsync(string hash, CancellationToken ct = default)
        => await Db.Merchants.Include(m => m.ApiKeys)
            .FirstOrDefaultAsync(m => m.ApiKeys.Any(k => k.KeyHash == hash && k.IsActive), ct);
 
    public async Task<Merchant?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await Db.Merchants
            .Include(m => m.ApiKeys)
            .FirstOrDefaultAsync(m => m.Email == email, ct);
 
    public async Task<bool> ExistsByEmailAsync(string email, CancellationToken ct = default)
        => await Db.Merchants.AnyAsync(m => m.Email == email, ct);
 
    public async Task<IReadOnlyList<Merchant>> GetByCountryAsync(string country, CancellationToken ct = default)
        => await Db.Merchants.Where(m => m.Country == country).OrderBy(m => m.BusinessName).ToListAsync(ct);

    public async Task<IReadOnlyList<Merchant>> GetActiveByPlanAsync(PricingPlan plan, CancellationToken ct = default)
    {
        return await Db.Merchants
            .Where(m => m.Status == MerchantStatus.Active)
            .Where(m => m.Plan == plan)
            .ToListAsync(ct);
    }
    public async Task<IReadOnlyList<Merchant>> ListAllAsync(
        MerchantStatus? status   = null,
        int             page     = 1,
        int             pageSize = 20,
        CancellationToken ct     = default)
    {
        var query = Db.Merchants.AsQueryable();

        if (status.HasValue)
            query = query.Where(m => m.Status == status.Value);

        return await query
            .OrderByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }
    
    public async Task<Merchant?> GetByRefreshTokenAsync(string token, CancellationToken ct = default)
        => await Db.Merchants.FirstOrDefaultAsync(m => m.RefreshToken == token, ct);
}