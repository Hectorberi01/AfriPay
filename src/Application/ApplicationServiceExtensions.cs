using AfriPay.Application.Admin;
using AfriPay.Application.Admin.Commands;
using AfriPay.Application.Admin.Queries;
using AfriPay.Application.Analytics;
using AfriPay.Application.Analytics.Queries;
using AfriPay.Application.Audit;
using AfriPay.Application.Audit.Queries;
using AfriPay.Application.Auth.Commands;
using AfriPay.Application.Balance;
using AfriPay.Application.Balance.Commands;
using AfriPay.Application.Balance.Queries;
using AfriPay.Application.Common.Behaviors;
using AfriPay.Application.Currency;
using AfriPay.Application.Currency.Commands;
using AfriPay.Application.Currency.Queries;
using AfriPay.Application.Disputes;
using AfriPay.Application.Disputes.Commands;
using AfriPay.Application.Disputes.Queries;
using AfriPay.Application.Employees;
using AfriPay.Application.Employees.Commands;
using AfriPay.Application.Employees.Queries;
using AfriPay.Application.Kyb;
using AfriPay.Application.Kyb.Commands;
using AfriPay.Application.Kyb.Queries;
using AfriPay.Application.Merchants;
using AfriPay.Application.Merchants.Commands.CreateMerchant;
using AfriPay.Application.Merchants.Commands.RegenerateApiKey;
using AfriPay.Application.Merchants.Commands.UpdateWebhook;
using AfriPay.Application.Merchants.Queries.GetMerchant;
using AfriPay.Application.Notifications;
using AfriPay.Application.Payments;
using AfriPay.Application.Payments.Commands.CancelPayment;
using AfriPay.Application.Payments.Commands.InitiPayment;
using AfriPay.Application.Payments.Queries.GetPayment;
using AfriPay.Application.Payments.Queries.ListPayments;
using AfriPay.Application.Payouts;
using AfriPay.Application.Payouts.Commands;
using AfriPay.Application.Payouts.Queries;
using AfriPay.Application.Refunds;
using AfriPay.Application.Refunds.Commands;
using AfriPay.Application.Refunds.Queries;
using AfriPay.Application.Subscriptions;
using AfriPay.Application.Subscriptions.Commands;
using AfriPay.Application.Subscriptions.Queries;
using AfriPay.Application.Webhook;
using AfriPay.Application.Webhook.Commands;
using AfriPay.Application.Webhook.Queries;
using FluentValidation;
using MediatR;

namespace AfriPay.Application;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(ApplicationServiceExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>),
                typeof(ValidationBehavior<,>));
        });

        // Merchant handlers 
        services.AddScoped<CreateMerchantHandler>();
        services.AddScoped<GetMerchantHandler>();
        services.AddScoped<UpdateWebhookHandler>();
        services.AddScoped<RegenerateApiKeyHandler>();
        services.AddScoped<IMerchantService, MerchantService>();

        // Payment handlers 
        services.AddScoped<InitiatePaymentHandler>();
        services.AddScoped<CancelPaymentHandler>();
        services.AddScoped<GetPaymentHandler>();
        services.AddScoped<ListPaymentsHandler>();
        services.AddScoped<IPaymentService, PaymentService>();
        
        // Refund handlers 
        services.AddScoped<CreateRefundHandler>();
        services.AddScoped<GetRefundHandler>();
        services.AddScoped<ListRefundsByPaymentHandler>();
        services.AddScoped<IRefundService, RefundService>();
        
        // Currency handlers 
        services.AddScoped<GetExchangeRateHandler>();
        services.AddScoped<ConvertCurrencyHandler>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        
        // Webhook handlers 
        services.AddScoped<ListWebhookDeliveriesHandler>();
        services.AddScoped<RetryWebhookDeliveryHandler>();
        services.AddScoped<IWebhookService, WebhookService>();
        
        // Balance handlers 
        services.AddScoped<GetBalanceHandler>();
        services.AddScoped<ListBalancesHandler>();
        services.AddScoped<ListBalanceEntriesHandler>();
        services.AddScoped<CreditPaymentHandler>();
        services.AddScoped<DebitRefundHandler>();
        services.AddScoped<DebitFeeHandler>();
        services.AddScoped<FreezeBalanceHandler>();
        services.AddScoped<UnfreezeBalanceHandler>();
        services.AddScoped<IBalanceService, BalanceService>();
 
        // KYB handlers 
        services.AddScoped<GetKybHandler>();
        services.AddScoped<InitKybHandler>();
        services.AddScoped<SetBusinessInfoHandler>();
        services.AddScoped<AddKybDocumentHandler>();
        services.AddScoped<SubmitKybHandler>();
        services.AddScoped<ListKybHandler>();
        services.AddScoped<ApproveKybHandler>();
        services.AddScoped<RejectKybHandler>();
        services.AddScoped<RequestAdditionalInfoHandler>();
        services.AddScoped<IKybService, KybService>();
        
        // Payout handlers 
        services.AddScoped<CreatePayoutHandler>();
        services.AddScoped<CancelPayoutHandler>();
        services.AddScoped<GetPayoutHandler>();
        services.AddScoped<ListPayoutsHandler>();
        services.AddScoped<IPayoutService, PayoutService>();
 
        // Audit handlers
        services.AddScoped<ListAuditEntriesHandler>();
        services.AddScoped<GetEntityAuditHandler>();
        services.AddScoped<IAuditService, AuditService>();
 
        // Analytics 
        services.AddScoped<GetAnalyticsSummaryHandler>();
        services.AddScoped<GetPayoutSummaryHandler>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
 
        // Subscriptions 
        services.AddScoped<CreatePlanHandler>();
        services.AddScoped<ListPlansHandler>();
        services.AddScoped<CreateSubscriptionHandler>();
        services.AddScoped<CancelSubscriptionHandler>();
        services.AddScoped<GetSubscriptionHandler>();
        services.AddScoped<ListSubscriptionsHandler>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        
        // Disputes 
        services.AddScoped<OpenDisputeHandler>();
        services.AddScoped<SubmitEvidenceHandler>();
        services.AddScoped<ResolveDisputeHandler>();
        services.AddScoped<GetDisputeHandler>();
        services.AddScoped<ListDisputesHandler>();
        services.AddScoped<IDisputeService, DisputeService>();
 
        // Admin 
        services.AddScoped<ListMerchantsAdminHandler>();
        services.AddScoped<SuspendMerchantHandler>();
        services.AddScoped<ReactivateMerchantHandler>();
        services.AddScoped<ChangePlanHandler>();
        services.AddScoped<IAdminService, AdminService>();
        
        services.AddScoped<RegisterHandler>();
        
        //team
        
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<CreateEmployeeHandler>();
        services.AddScoped<ReactivateEmployeeHandler>();
        services.AddScoped<SuspendEmployeeHandler>();
        services.AddScoped<UpdateEmployeeHandler>();
        services.AddScoped<GetEmployeeHandler>();
        services.AddScoped<ListEmployeesHandler>();
        services.AddScoped<EmployeeLoginHandler>();
 
        // Notifications 
        services.AddScoped<INotificationService, NotificationService>();

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}