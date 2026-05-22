using AfriPay.Domain.Exceptions;
using AfriPay.Domain.Repositories;

namespace AfriPay.Infrastructure.Jobs;

/// <summary>
/// Settle les paiements complétés il y a plus de T+1 (24h).
/// Transfère le solde de PendingBalance vers AvailableBalance.
/// </summary>
public sealed class SettlementJob(
    IServiceScopeFactory      scopeFactory,
    ILogger<SettlementJob>    logger)
    : AfriPayJob(scopeFactory, logger, "SettlementJob")
{
    // Tourne toutes les heures
    protected override TimeSpan GetInterval() => TimeSpan.FromHours(1);
 
    protected override async Task RunAsync(IServiceProvider sp, CancellationToken ct)
    {
        var uow = sp.GetRequiredService<IUnitOfWork>();
 
        // Chercher les paiements complétés il y a plus de 24h non encore settlés
        var cutoff  = DateTimeOffset.UtcNow.AddHours(-24);
        // GetCompletedBeforeAsync — à implémenter dans PaymentRepository
        // Pour l'instant filtre via ListAsync avec un filtre date
        var settled = await uow.Payments.GetSettlementPendingAsync(cutoff, ct);
 
        if (!settled.Any())
        {
            logger.LogDebug("SettlementJob: nothing to settle.");
            return;
        }
 
        logger.LogInformation("SettlementJob: settling {Count} payments.", settled.Count);
 
        var settledCount = 0;
        foreach (var payment in settled)
        {
            var balance = await uow.MerchantBalances
                .GetByMerchantAndCurrencyAsync(
                    payment.MerchantId,
                    payment.Amount.Currency, ct);
 
            if (balance is null) continue;
 
            try
            {
                // Montant net (après commission)
                var netAmount = payment.Net?.Amount ?? payment.Amount.Amount;
                var entry     = balance.Settle(netAmount, payment.Id.ToString());
 
                uow.MerchantBalances.Update(balance);
                await uow.BalanceEntries.AddAsync(entry, ct);
                settledCount++;
            }
            catch (DomainException ex)
            {
                logger.LogWarning(ex,
                    "SettlementJob: failed to settle payment {Id}.", payment.Id);
            }
        }
 
        if (settledCount > 0)
            await uow.SaveChangesAsync(ct);
 
        logger.LogInformation("SettlementJob: {Count} payments settled.", settledCount);
    }
}
