using AfriPay.Domain.Staff;

namespace AfriPay.Domain.Repositories;

public interface IStaffRepository
{
    Task<StaffMember?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<StaffMember?> GetByEmailAsync(string email, CancellationToken ct = default);
    Task<StaffMember?> GetByRefreshTokenAsync(string token, CancellationToken ct = default);
    Task<IReadOnlyList<StaffMember>> ListAsync(StaffRole? role, StaffStatus? status, CancellationToken ct = default);
    Task AddAsync(StaffMember staff, CancellationToken ct = default);
    void Update(StaffMember staff);
}