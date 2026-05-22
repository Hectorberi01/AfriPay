using AfriPay.Application.Balance.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Repositories;
using static AfriPay.Application.Balance.Dtos.BalanceDtoMapper;

namespace AfriPay.Application.Balance.Commands;

public sealed record DebitRefundCommand(
    Guid   MerchantId,
    string RefundId,
    long   Amount,
    string Currency);


public sealed class DebitRefundHandler(IUnitOfWork uow)
{
    public async Task<Result<BalanceSummaryDto>> HandleAsync(
        DebitRefundCommand cmd, CancellationToken ct = default)
    {
        var balance = await uow.MerchantBalances
            .GetByMerchantAndCurrencyAsync(cmd.MerchantId, cmd.Currency, ct);
 
        if (balance is null)
            return Result<BalanceSummaryDto>.Fail(
                AppError.NotFound("Balance", $"{cmd.MerchantId}/{cmd.Currency}"));
 
        try
        {
            var entry = balance.DebitForRefund(cmd.Amount, cmd.RefundId);
 
            uow.MerchantBalances.Update(balance);
            await uow.BalanceEntries.AddAsync(entry, ct);
            await uow.SaveChangesAsync(ct);
 
            return Result<BalanceSummaryDto>.Ok(ToDto(balance));
        }
        catch (DomainException ex)
        {
            return Result<BalanceSummaryDto>.Fail(AppError.DomainRule(ex.Message));
        }
    }
}