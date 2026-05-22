using AfriPay.Application.Balance.Commands;
using AfriPay.Application.Balance.Dtos;
using AfriPay.Application.Balance.Queries;
using AfriPay.Application.Common.Errors;

namespace AfriPay.Application.Balance;

public interface IBalanceService
{
    Task<Result<BalanceSummaryDto>>            GetBalanceAsync(GetBalanceQuery q,            CancellationToken ct = default);
    Task<Result<IReadOnlyList<BalanceSummaryDto>>> ListBalancesAsync(ListBalancesQuery q,     CancellationToken ct = default);
    Task<Result<IReadOnlyList<BalanceEntryDto>>>   GetEntriesAsync(ListBalanceEntriesQuery q, CancellationToken ct = default);
    Task<Result<BalanceSummaryDto>>            CreditPaymentAsync(CreditPaymentCommand cmd,   CancellationToken ct = default);
    Task<Result<BalanceSummaryDto>>            DebitRefundAsync(DebitRefundCommand cmd,        CancellationToken ct = default);
    Task<Result<bool>>                         DebitFeeAsync(DebitFeeCommand cmd,             CancellationToken ct = default);
    Task<Result<BalanceSummaryDto>>            FreezeAsync(FreezeBalanceCommand cmd,          CancellationToken ct = default);
    Task<Result<BalanceSummaryDto>>            UnfreezeAsync(UnfreezeBalanceCommand cmd,      CancellationToken ct = default);
}
 
public sealed class BalanceService(
    GetBalanceHandler          getHandler,
    ListBalancesHandler        listHandler,
    ListBalanceEntriesHandler  entriesHandler,
    CreditPaymentHandler       creditHandler,
    DebitRefundHandler         debitRefundHandler,
    DebitFeeHandler            debitFeeHandler,
    FreezeBalanceHandler       freezeHandler,
    UnfreezeBalanceHandler     unfreezeHandler) : IBalanceService
{
    public Task<Result<BalanceSummaryDto>>               GetBalanceAsync(GetBalanceQuery q,            CancellationToken ct = default) => getHandler.HandleAsync(q, ct);
    public Task<Result<IReadOnlyList<BalanceSummaryDto>>> ListBalancesAsync(ListBalancesQuery q,        CancellationToken ct = default) => listHandler.HandleAsync(q, ct);
    public Task<Result<IReadOnlyList<BalanceEntryDto>>>   GetEntriesAsync(ListBalanceEntriesQuery q,   CancellationToken ct = default) => entriesHandler.HandleAsync(q, ct);
    public Task<Result<BalanceSummaryDto>>               CreditPaymentAsync(CreditPaymentCommand cmd,  CancellationToken ct = default) => creditHandler.HandleAsync(cmd, ct);
    public Task<Result<BalanceSummaryDto>>               DebitRefundAsync(DebitRefundCommand cmd,       CancellationToken ct = default) => debitRefundHandler.HandleAsync(cmd, ct);
    public Task<Result<bool>>                            DebitFeeAsync(DebitFeeCommand cmd,            CancellationToken ct = default) => debitFeeHandler.HandleAsync(cmd, ct);
    public Task<Result<BalanceSummaryDto>>               FreezeAsync(FreezeBalanceCommand cmd,         CancellationToken ct = default) => freezeHandler.HandleAsync(cmd, ct);
    public Task<Result<BalanceSummaryDto>>               UnfreezeAsync(UnfreezeBalanceCommand cmd,     CancellationToken ct = default) => unfreezeHandler.HandleAsync(cmd, ct);
}