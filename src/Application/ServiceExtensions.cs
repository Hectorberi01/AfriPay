namespace AfriPay.Application.Notifications;

public static class ServiceExtensions
{
    public static IServiceCollection NotificationExtention(
        this IServiceCollection services,
        IConfiguration          configuration)
    {
        // Email
        services.AddScoped<IEmailSender, SendGridEmailSender>();
        services.AddScoped<INotificationService,
            AfriPay.Application.Notifications.NotificationService>();
 
        return services;
    }
}