namespace AfriPay.Infrastructure.Jobs;

/// <summary>
/// Base pour tous les background jobs AfriPay.
/// Crée un scope DI à chaque exécution pour accéder aux services Scoped
/// (IUnitOfWork, DbContext…) depuis un Singleton BackgroundService.
/// </summary>
public abstract class AfriPayJob(
    IServiceScopeFactory  scopeFactory,
    ILogger               logger,
    string                jobName) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Job {Job} started.", jobName);
 
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(GetInterval(), stoppingToken);
 
            try
            {
                await using var scope = scopeFactory.CreateAsyncScope();
                await RunAsync(scope.ServiceProvider, stoppingToken);
            }
            catch (OperationCanceledException) { break; }
            catch (Exception ex)
            {
                logger.LogError(ex, "Job {Job} failed.", jobName);
            }
        }
 
        logger.LogInformation("Job {Job} stopped.", jobName);
    }
 
    protected abstract TimeSpan GetInterval();
    protected abstract Task RunAsync(IServiceProvider sp, CancellationToken ct);
}