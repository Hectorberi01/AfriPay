using AfriPay.Domain.Employees;

namespace AfriPay.Domain.Repositories;

public interface IEmployeeRepository
{
    Task<Employee?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<Employee?> GetByEmailAsync(
        Guid merchantId, string email, CancellationToken ct = default);

    Task<Employee?> GetByEmailGlobalAsync(
        string email, CancellationToken ct = default);

    Task<Employee?> GetByRefreshTokenAsync(
        string token, CancellationToken ct = default);

    Task<IReadOnlyList<Employee>> GetByMerchantAsync(
        Guid merchantId, EmployeeRole? role = null, CancellationToken ct = default);

    Task AddAsync(Employee employee, CancellationToken ct = default);

    void Update(Employee employee);
}