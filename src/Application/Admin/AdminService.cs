using AfriPay.Application.Admin.Commands;
using AfriPay.Application.Admin.Dtos;
using AfriPay.Application.Admin.Queries;
using AfriPay.Application.Common.Errors;

namespace AfriPay.Application.Admin;

public interface IAdminService
{
    Task<Result<IReadOnlyList<AdminMerchantDto>>> ListMerchantsAsync(ListMerchantsAdminQuery q, CancellationToken ct = default);
    Task<Result<bool>> SuspendMerchantAsync(SuspendMerchantCommand cmd,    CancellationToken ct = default);
    Task<Result<bool>> ReactivateMerchantAsync(ReactivateMerchantCommand cmd, CancellationToken ct = default);
    Task<Result<bool>> ChangePlanAsync(ChangePlanCommand cmd,               CancellationToken ct = default);
}
 
public sealed class AdminService(
    ListMerchantsAdminHandler  listHandler,
    SuspendMerchantHandler     suspendHandler,
    ReactivateMerchantHandler  reactivateHandler,
    ChangePlanHandler          planHandler) : IAdminService
{
    public Task<Result<IReadOnlyList<AdminMerchantDto>>> ListMerchantsAsync(ListMerchantsAdminQuery q, CancellationToken ct = default) => listHandler.HandleAsync(q, ct);
    public Task<Result<bool>> SuspendMerchantAsync(SuspendMerchantCommand cmd,    CancellationToken ct = default) => suspendHandler.HandleAsync(cmd, ct);
    public Task<Result<bool>> ReactivateMerchantAsync(ReactivateMerchantCommand cmd, CancellationToken ct = default) => reactivateHandler.HandleAsync(cmd, ct);
    public Task<Result<bool>> ChangePlanAsync(ChangePlanCommand cmd,               CancellationToken ct = default) => planHandler.HandleAsync(cmd, ct);
}