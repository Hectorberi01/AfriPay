using AfriPay.Application.Balance.Dtos;
using AfriPay.Application.Common.Errors;
using AfriPay.Domain.Balance;
using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Repositories;
using static AfriPay.Application.Balance.Dtos.BalanceDtoMapper;


namespace AfriPay.Application.Balance.Commands;

public sealed record CreditPaymentCommand(
    Guid   MerchantId,
    string PaymentId,
    long   Amount,
    string Currency);

public sealed class CreditPaymentHandler(IUnitOfWork uow)
{
    public async Task<Result<BalanceSummaryDto>> HandleAsync(
        CreditPaymentCommand cmd, CancellationToken ct = default)
    {
        var balance = await GetOrCreateAsync(cmd.MerchantId, cmd.Currency, ct);
 
        try
        {
            var entry = balance.CreditPending(cmd.Amount, cmd.PaymentId, "Payment received");
 
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
 
    private async Task<MerchantBalance> GetOrCreateAsync(
        Guid merchantId, string currency, CancellationToken ct)
    {
        var balance = await uow.MerchantBalances
            .GetByMerchantAndCurrencyAsync(merchantId, currency, ct);
 
        if (balance is not null) return balance;
 
        balance = MerchantBalance.Create(merchantId, currency);
        await uow.MerchantBalances.AddAsync(balance, ct);
        return balance;
    }
}