namespace AfriPay.Infrastructure.Jobs;

public static class JobServiceExtensions
{
    public static IServiceCollection AddAfriPayJobs(this IServiceCollection services)
    {
        services.AddHostedService<ExpiryJob>();
        services.AddHostedService<WebhookDispatcherJob>();
        services.AddHostedService<SettlementJob>();
        services.AddHostedService<PayoutSchedulerJob>();
 
        // HttpClient pour les webhooks sortants
        services.AddHttpClient("webhook", c =>
        {
            c.Timeout = TimeSpan.FromSeconds(15);
            c.DefaultRequestHeaders.Add("User-Agent", "AfriPay-Webhook/1.0");
        });
 
        return services;
    }
}