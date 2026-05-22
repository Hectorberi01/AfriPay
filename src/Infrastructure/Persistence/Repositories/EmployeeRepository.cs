using AfriPay.Domain.Employees;
using AfriPay.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class EmployeeRepository(AfriPayDbContextBase db)
    : IEmployeeRepository
{
    public async Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<Employee>().FirstOrDefaultAsync(e => e.Id == id, ct);
 
    public async Task<Employee?> GetByEmailAsync(
        Guid merchantId, string email, CancellationToken ct = default)
        => await db.Set<Employee>()
            .FirstOrDefaultAsync(e =>
                e.MerchantId == merchantId &&
                e.Email      == email.ToLowerInvariant(), ct);
 
    public async Task<Employee?> GetByEmailGlobalAsync(
        string email, CancellationToken ct = default)
        => await db.Set<Employee>()
            .FirstOrDefaultAsync(e => e.Email == email.ToLowerInvariant(), ct);
 
    public async Task<Employee?> GetByRefreshTokenAsync(
        string token, CancellationToken ct = default)
        => await db.Set<Employee>()
            .FirstOrDefaultAsync(e => e.RefreshToken == token, ct);
 
    public async Task<IReadOnlyList<Employee>> GetByMerchantAsync(
        Guid merchantId, EmployeeRole? role = null, CancellationToken ct = default)
    {
        var q = db.Set<Employee>().Where(e => e.MerchantId == merchantId);
        if (role.HasValue) q = q.Where(e => e.Role == role.Value);
        return await q.OrderBy(e => e.Name).ToListAsync(ct);
    }
 
    public async Task AddAsync(Employee employee, CancellationToken ct = default)
        => await db.Set<Employee>().AddAsync(employee, ct);
 
    public void Update(Employee employee)
        => db.Set<Employee>().Update(employee);
}