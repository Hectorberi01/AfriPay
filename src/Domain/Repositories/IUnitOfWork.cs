namespace AfriPay.Domain.Repositories;

/// <summary>
/// Agrège tous les repositories et garantit l'atomicité des commits.
/// Pattern UoW : tous les repositories partagent la même connexion
/// base de données dans la durée de vie d'une requête HTTP (Scoped).
/// SaveChangesAsync() committe tous les changements en une seule transaction.
/// Pour les opérations multi-agrégats (ex : créer un Payment ET un WebhookDelivery
/// dans la même transaction), utiliser ExecuteInTransactionAsync().
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    IMerchantRepository        Merchants         { get; }
    IPaymentRepository         Payments          { get; }
    IRefundRepository          Refunds           { get; }
    IWebhookDeliveryRepository WebhookDeliveries { get; }
    IExchangeRateRepository    ExchangeRates     { get; }
    
    IMerchantBalanceRepository MerchantBalances  { get; }
    IBalanceEntryRepository    BalanceEntries    { get; }
    IFeeRuleRepository         FeeRules          { get; }
    IKybRepository             Kyb               { get; }
    
    IAuditRepository             Audit               { get; }
    IPayoutRepository            Payouts             { get; }
    
    ISubscriptionRepository        Subscriptions          { get; }
    ISubscriptionPlanRepository        SubscriptionPlans        { get; }
    
    IDisputeRepository               Disputes { get; }
    
    IEmployeeRepository             Employees         { get; }
    
    IStaffRepository               Staff              { get; }
 
    /// <summary>
    /// Persiste tous les changements trackés en une transaction implicite.
    /// Retourne le nombre d'entités affectées.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken ct = default);
 
    /// <summary>
    /// Exécute une fonction dans une transaction PostgreSQL explicite.
    ///
    /// Utiliser quand :
    ///   - On touche plusieurs agrégats (Payment + WebhookDelivery outbox)
    ///   - On a besoin de rollback conditionnel
    ///   - On veut garantir l'isolation read-committed ou supérieure
    ///
    /// Exemple :
    /// <code>
    /// await uow.ExecuteInTransactionAsync(async () =>
    /// {
    ///     await uow.Payments.AddAsync(payment, ct);
    ///     await uow.WebhookDeliveries.AddAsync(delivery, ct);
    ///     // SaveChangesAsync appelé automatiquement à la fin
    /// }, ct);
    /// </code>
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default);
 
    /// <summary>
    /// Surcharge avec valeur de retour pour les transactions qui produisent un résultat.
    /// </summary>
    Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default);
}