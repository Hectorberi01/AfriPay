using AfriPay.Application.Common.Errors;
using AfriPay.Application.Subscriptions.Commands;
using AfriPay.Application.Subscriptions.Dtos;
using AfriPay.Application.Subscriptions.Queries;

namespace AfriPay.Application.Subscriptions;

public interface ISubscriptionService
{
    Task<Result<SubscriptionPlanDto>>                    CreatePlanAsync(CreatePlanCommand cmd,            CancellationToken ct = default);
    Task<Result<IReadOnlyList<SubscriptionPlanDto>>>     ListPlansAsync(ListPlansQuery query,               CancellationToken ct = default);
    Task<Result<SubscriptionDto>>                        CreateAsync(CreateSubscriptionCommand cmd,         CancellationToken ct = default);
    Task<Result<SubscriptionDto>>                        CancelAsync(CancelSubscriptionCommand cmd,         CancellationToken ct = default);
    Task<Result<SubscriptionDto>>                        GetAsync(GetSubscriptionQuery query,               CancellationToken ct = default);
    Task<Result<IReadOnlyList<SubscriptionDto>>>         ListAsync(ListSubscriptionsQuery query,            CancellationToken ct = default);
}
 
public sealed class SubscriptionService(
    CreatePlanHandler          createPlanHandler,
    ListPlansHandler           listPlansHandler,
    CreateSubscriptionHandler  createHandler,
    CancelSubscriptionHandler  cancelHandler,
    GetSubscriptionHandler     getHandler,
    ListSubscriptionsHandler   listHandler) : ISubscriptionService
{
    public Task<Result<SubscriptionPlanDto>>                CreatePlanAsync(CreatePlanCommand cmd, CancellationToken ct = default) => createPlanHandler.HandleAsync(cmd, ct);
    public Task<Result<IReadOnlyList<SubscriptionPlanDto>>> ListPlansAsync(ListPlansQuery q,        CancellationToken ct = default) => listPlansHandler.HandleAsync(q, ct);
    public Task<Result<SubscriptionDto>>                    CreateAsync(CreateSubscriptionCommand cmd, CancellationToken ct = default) => createHandler.HandleAsync(cmd, ct);
    public Task<Result<SubscriptionDto>>                    CancelAsync(CancelSubscriptionCommand cmd, CancellationToken ct = default) => cancelHandler.HandleAsync(cmd, ct);
    public Task<Result<SubscriptionDto>>                    GetAsync(GetSubscriptionQuery q,         CancellationToken ct = default) => getHandler.HandleAsync(q, ct);
    public Task<Result<IReadOnlyList<SubscriptionDto>>>     ListAsync(ListSubscriptionsQuery q,      CancellationToken ct = default) => listHandler.HandleAsync(q, ct);
}