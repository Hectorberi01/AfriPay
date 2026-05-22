using AfriPay.Domain.Repositories;

namespace AfriPay.Infrastructure.Jobs;

public sealed class ExpiryJob(
    IServiceScopeFactory scopeFactory,
    ILogger<ExpiryJob>   logger)
    : AfriPayJob(scopeFactory, logger, "ExpiryJob")
{
    // Tourne toutes les 5 minutes
    protected override TimeSpan GetInterval() => TimeSpan.FromMinutes(5);
 
    protected override async Task RunAsync(IServiceProvider sp, CancellationToken ct)
    {
        var uow = sp.GetRequiredService<IUnitOfWork>();
 
        var expired = await uow.Payments.GetExpiredPendingAsync(ct: ct);
 
        if (!expired.Any())
        {
            logger.LogDebug("ExpiryJob: no expired payments.");
            return;
        }
 
        logger.LogInformation("ExpiryJob: expiring {Count} payments.", expired.Count);
 
        foreach (var payment in expired)
        {
            try
            {
                payment.MarkExpired();
                uow.Payments.Update(payment);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex,
                    "ExpiryJob: could not expire payment {Id}.", payment.Id);
            }
        }
 
        await uow.SaveChangesAsync(ct);
        logger.LogInformation("ExpiryJob: {Count} payments expired.", expired.Count);
    }
}