using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payouts.Dtos;
using AfriPay.Domain.Audit;
using AfriPay.Domain.Payouts;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Payouts.Commands;

public sealed record CancelPayoutCommand(Guid PayoutId, Guid MerchantId, string? Reason);
 
public sealed class CancelPayoutHandler(IUnitOfWork uow)
{
    public async Task<Result<PayoutDto>> HandleAsync(
        CancelPayoutCommand cmd, CancellationToken ct = default)
    {
        var payout = await uow.Payouts.GetByIdAsync(cmd.PayoutId, ct);
 
        if (payout is null)
            return Result<PayoutDto>.Fail(
                AppError.NotFound("Payout", cmd.PayoutId.ToString()));
 
        if (payout.MerchantId != cmd.MerchantId)
            return Result<PayoutDto>.Fail(AppError.Unauthorized());
 
        try { payout.Cancel(cmd.Reason); }
        catch (PayoutDomainException ex)
        {
            return Result<PayoutDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        // Récréditer le balance
        var balance = await uow.MerchantBalances
            .GetByMerchantAndCurrencyAsync(cmd.MerchantId, payout.Currency, ct);
 
        await uow.ExecuteInTransactionAsync(async () =>
        {
            uow.Payouts.Update(payout);
 
            if (balance is not null)
            {
                var entry = balance.CreditPending(
                    payout.Amount, payout.Id.ToString(), "Payout cancelled — balance restored");
                uow.MerchantBalances.Update(balance);
                await uow.BalanceEntries.AddAsync(entry, ct);
            }
 
            await uow.Audit.AddAsync(AuditEntry.Create(
                cmd.MerchantId, AuditAction.PayoutCancelled,
                "Payout", payout.Id.ToString()), ct);
        }, ct);
 
        return Result<PayoutDto>.Ok(PayoutDto.FromDomain(payout));
    }
}