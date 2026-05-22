using AfriPay.Domain.Payouts;
using AfriPay.Domain.Repositories;

namespace AfriPay.Infrastructure.Jobs;

/// <summary>
/// Traite les payouts en statut Pending.
/// En production, délègue à l'adapter provider (MTN, Wave…).
/// </summary>
public sealed class PayoutSchedulerJob(
    IServiceScopeFactory         scopeFactory,
    ILogger<PayoutSchedulerJob>  logger)
    : AfriPayJob(scopeFactory, logger, "PayoutSchedulerJob")
{
    // Tourne toutes les 15 minutes
    protected override TimeSpan GetInterval() => TimeSpan.FromMinutes(15);
 
    protected override async Task RunAsync(IServiceProvider sp, CancellationToken ct)
    {
        var uow = sp.GetRequiredService<IUnitOfWork>();
 
        var pending = await uow.Payouts.GetPendingAsync(ct);
 
        if (!pending.Any())
        {
            logger.LogDebug("PayoutScheduler: no pending payouts.");
            return;
        }
 
        logger.LogInformation(
            "PayoutScheduler: processing {Count} payouts.", pending.Count);
 
        foreach (var payout in pending)
        {
            await ProcessPayoutAsync(payout, uow, ct);
        }
 
        await uow.SaveChangesAsync(ct);
    }
 
    private async Task ProcessPayoutAsync(
        Payout            payout,
        IUnitOfWork       uow,
        CancellationToken ct)
    {
        try
        {
            payout.MarkProcessing();
 
            // TODO Phase 5 : appeler le provider adapter selon payout.Method
            // Pour l'instant : simuler un succès en sandbox
            var providerReference = $"PAYOUT-{Guid.NewGuid().ToString()[..8].ToUpper()}";
            payout.MarkCompleted(providerReference);
 
            uow.Payouts.Update(payout);
 
            logger.LogInformation(
                "PayoutScheduler: payout {Id} completed → {Ref}",
                payout.Id, providerReference);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "PayoutScheduler: payout {Id} failed.", payout.Id);
 
            try
            {
                payout.MarkFailed(ex.Message);
                uow.Payouts.Update(payout);
 
                // Récréditer le balance
                var balance = await uow.MerchantBalances
                    .GetByMerchantAndCurrencyAsync(
                        payout.MerchantId, payout.Currency, ct);
 
                if (balance is not null)
                {
                    var entry = balance.CreditPending(
                        payout.Amount, payout.Id.ToString(),
                        "Payout failed — balance restored");
                    uow.MerchantBalances.Update(balance);
                    await uow.BalanceEntries.AddAsync(entry, ct);
                }
            }
            catch (Exception inner)
            {
                logger.LogCritical(inner,
                    "PayoutScheduler: failed to rollback payout {Id}.", payout.Id);
            }
        }
    }
}