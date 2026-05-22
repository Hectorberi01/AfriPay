using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payouts.Commands;
using AfriPay.Application.Payouts.Dtos;
using AfriPay.Application.Payouts.Queries;

namespace AfriPay.Application.Payouts;

public interface IPayoutService
{
    Task<Result<PayoutDto>>                    CreateAsync(CreatePayoutCommand cmd,  CancellationToken ct = default);
    Task<Result<PayoutDto>>                    CancelAsync(CancelPayoutCommand cmd,  CancellationToken ct = default);
    Task<Result<PayoutDto>>                    GetAsync(GetPayoutQuery query,         CancellationToken ct = default);
    Task<Result<IReadOnlyList<PayoutDto>>>     ListAsync(ListPayoutsQuery query,      CancellationToken ct = default);
}
 
public sealed class PayoutService(
    CreatePayoutHandler  createHandler,
    CancelPayoutHandler  cancelHandler,
    GetPayoutHandler     getHandler,
    ListPayoutsHandler   listHandler) : IPayoutService
{
    public Task<Result<PayoutDto>>                CreateAsync(CreatePayoutCommand cmd,  CancellationToken ct = default) => createHandler.HandleAsync(cmd, ct);
    public Task<Result<PayoutDto>>                CancelAsync(CancelPayoutCommand cmd,  CancellationToken ct = default) => cancelHandler.HandleAsync(cmd, ct);
    public Task<Result<PayoutDto>>                GetAsync(GetPayoutQuery query,         CancellationToken ct = default) => getHandler.HandleAsync(query, ct);
    public Task<Result<IReadOnlyList<PayoutDto>>> ListAsync(ListPayoutsQuery query,      CancellationToken ct = default) => listHandler.HandleAsync(query, ct);
}