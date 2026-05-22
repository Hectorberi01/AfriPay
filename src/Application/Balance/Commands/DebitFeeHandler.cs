using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Balance.Commands;

public sealed record DebitFeeCommand(
    Guid   MerchantId,
    string PaymentId,
    long   FeeAmount,
    string Currency);
 
public sealed class DebitFeeHandler(IUnitOfWork uow)
{
    public async Task<Result<bool>> HandleAsync(
        DebitFeeCommand cmd, CancellationToken ct = default)
    {
        if (cmd.FeeAmount == 0) return Result<bool>.Ok(true);
 
        var balance = await uow.MerchantBalances
            .GetByMerchantAndCurrencyAsync(cmd.MerchantId, cmd.Currency, ct);
 
        if (balance is null) return Result<bool>.Ok(false);
 
        try
        {
            var entry = balance.DebitFee(cmd.FeeAmount, cmd.PaymentId);
 
            uow.MerchantBalances.Update(balance);
            await uow.BalanceEntries.AddAsync(entry, ct);
            await uow.SaveChangesAsync(ct);
 
            return Result<bool>.Ok(true);
        }
        catch (DomainException ex)
        {
            return Result<bool>.Fail(AppError.DomainRule(ex.Message));
        }
    }
}
