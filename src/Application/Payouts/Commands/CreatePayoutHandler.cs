using AfriPay.Application.Common.Errors;
using AfriPay.Application.Payouts.Dtos;
using AfriPay.Domain.Audit;
using AfriPay.Domain.Payouts;
using AfriPay.Domain.Repositories;

namespace AfriPay.Application.Payouts.Commands;

public sealed record CreatePayoutCommand(
    Guid          MerchantId,
    long          Amount,
    string        Currency,
    string        Method,         // "mobile_money" | "bank_transfer"
    string?       ProviderKey,    // "mtn_momo", "wave"…
    string?       PhoneNumber,
    string?       Iban,
    string?       Bic,
    string?       AccountHolder,
    string?       BankName,
    string?       Notes);

public sealed class CreatePayoutHandler(IUnitOfWork uow)
{
    public async Task<Result<PayoutDto>> HandleAsync(
        CreatePayoutCommand cmd, CancellationToken ct = default)
    {
        // Vérifier le solde disponible
        var balance = await uow.MerchantBalances
            .GetByMerchantAndCurrencyAsync(cmd.MerchantId, cmd.Currency, ct);
 
        if (balance is null || !balance.HasSufficientFunds(cmd.Amount))
            return Result<PayoutDto>.Fail(
                AppError.Conflict(
                    $"Insufficient available balance. " +
                    $"Available: {balance?.AvailableBalance ?? 0} {cmd.Currency}, " +
                    $"Requested: {cmd.Amount} {cmd.Currency}."));
 
        // Construire la destination
        PayoutDestination destination;
        try
        {
            destination = cmd.Method.ToLower() switch
            {
                "mobile_money" => PayoutDestination.ForMobileMoney(
                    cmd.ProviderKey ?? throw new PayoutDomainException("ProviderKey required for Mobile Money."),
                    cmd.PhoneNumber ?? throw new PayoutDomainException("Phone number required.")),
 
                "bank_transfer" => PayoutDestination.ForBankTransfer(
                    BankAccount.Create(
                        cmd.Iban          ?? throw new PayoutDomainException("IBAN required."),
                        cmd.Bic           ?? "",
                        cmd.AccountHolder ?? throw new PayoutDomainException("Account holder required."),
                        cmd.BankName      ?? "",
                        "FR")),
 
                _ => throw new PayoutDomainException($"Unknown method: {cmd.Method}."),
            };
        }
        catch (PayoutDomainException ex)
        {
            return Result<PayoutDto>.Fail(AppError.Validation("destination", ex.Message));
        }
 
        // Créer le payout
        Payout payout;
        try
        {
            payout = Payout.Create(
                cmd.MerchantId, cmd.Amount, cmd.Currency,
                destination, PayoutSchedule.Manual, cmd.Notes);
        }
        catch (PayoutDomainException ex)
        {
            return Result<PayoutDto>.Fail(AppError.DomainRule(ex.Message));
        }
 
        // Débiter le balance
        var entry = balance.DebitForPayout(cmd.Amount, payout.Id.ToString());
 
        await uow.ExecuteInTransactionAsync(async () =>
        {
            await uow.Payouts.AddAsync(payout, ct);
            uow.MerchantBalances.Update(balance);
            await uow.BalanceEntries.AddAsync(entry, ct);
 
            // Audit
            await uow.Audit.AddAsync(AuditEntry.Create(
                cmd.MerchantId, AuditAction.PayoutRequested,
                "Payout", payout.Id.ToString(),
                metadata: $"{{\"amount\":{cmd.Amount},\"currency\":\"{cmd.Currency}\"}}"), ct);
        }, ct);
 
        return Result<PayoutDto>.Ok(PayoutDto.FromDomain(payout));
    }
}