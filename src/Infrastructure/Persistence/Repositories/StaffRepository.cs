using AfriPay.Domain.Repositories;
using AfriPay.Domain.Staff;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence.Repositories;

internal sealed class StaffRepository(AfriPayDbContextBase db) : IStaffRepository
{
    public async Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await db.Set<StaffMember>().FirstOrDefaultAsync(s => s.Id == id, ct);
 
    public async Task<StaffMember?> GetByEmailAsync(string email, CancellationToken ct = default)
        => await db.Set<StaffMember>()
            .FirstOrDefaultAsync(s => s.Email == email.ToLowerInvariant(), ct);
 
    public async Task<StaffMember?> GetByRefreshTokenAsync(string token, CancellationToken ct = default)
        => await db.Set<StaffMember>()
            .FirstOrDefaultAsync(s => s.RefreshToken == token, ct);
 
    public async Task<IReadOnlyList<StaffMember>> ListAsync(
        StaffRole? role, StaffStatus? status, CancellationToken ct = default)
    {
        var q = db.Set<StaffMember>().AsQueryable();
        if (role.HasValue)   q = q.Where(s => s.Role   == role.Value);
        if (status.HasValue) q = q.Where(s => s.Status == status.Value);
        return await q.OrderBy(s => s.Name).ToListAsync(ct);
    }
 
    public async Task AddAsync(StaffMember staff, CancellationToken ct = default)
        => await db.Set<StaffMember>().AddAsync(staff, ct);
 
    public void Update(StaffMember staff)
        => db.Set<StaffMember>().Update(staff);
}