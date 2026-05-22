using AfriPay.Domain.Repositories;
using AfriPay.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace AfriPay.Infrastructure.Persistence;

/// <summary>
/// Implémentation de l'Unit of Work.
/// Durée de vie : Scoped — une instance par requête HTTP.
/// Tous les repositories partagent la même instance de AfriPayDbContext,
/// ce qui garantit que SaveChangesAsync() committe tout en une seule transaction.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly AfriPayDbContextBase _db;
 
    // Lazy init — instanciés uniquement si accédés dans la requête
    private IMerchantRepository?        _merchants;
    private IPaymentRepository?         _payments;
    private IRefundRepository?          _refunds;
    private IWebhookDeliveryRepository? _webhooks;
    private IExchangeRateRepository?    _rates;
    
    // Phase 2
    private IMerchantBalanceRepository? _balances;
    private IBalanceEntryRepository?    _balanceEntries;
    private IFeeRuleRepository?         _feeRules;
    private IKybRepository?             _kyb;
    
    private IAuditRepository?            _audits;
    private IPayoutRepository?           _payouts;
    
    private ISubscriptionRepository?     _subscriptions;
    private ISubscriptionPlanRepository? _subscriptionPlans;
    
    private IDisputeRepository?         _disputes;
    
    private IEmployeeRepository?         _employees;
    
    private IStaffRepository           _staff;
 
    public UnitOfWork(IDbContextFactory factory, IRequestEnvironment env)
    {
        _db = factory.Resolve(env.IsLive);
    }
 
    public IMerchantRepository        Merchants => _merchants ??= new MerchantRepository(_db);
 
    public IPaymentRepository         Payments => _payments  ??= new PaymentRepository(_db);
 
    public IRefundRepository          Refunds => _refunds   ??= new RefundRepository(_db);
 
    public IWebhookDeliveryRepository WebhookDeliveries => _webhooks  ??= new WebhookDeliveryRepository(_db);
 
    public IExchangeRateRepository    ExchangeRates => _rates     ??= new ExchangeRateRepository(_db);
    
    // Phase 2
    public IMerchantBalanceRepository MerchantBalances => _balances      ??= new MerchantBalanceRepository(_db);
 
    public IBalanceEntryRepository    BalanceEntries => _balanceEntries ??= new BalanceEntryRepository(_db);
 
    public IFeeRuleRepository         FeeRules => _feeRules      ??= new FeeRuleRepository(_db);
 
    public IKybRepository             Kyb => _kyb           ??= new KybRepository(_db);
    
    public IAuditRepository         Audit => _audits        ??= new AuditRepository(_db);
    public IPayoutRepository        Payouts => _payouts        ??= new PayoutRepository(_db);
    
    public ISubscriptionRepository Subscriptions => _subscriptions ??= new SubscriptionRepository(_db);
    public ISubscriptionPlanRepository SubscriptionPlans => _subscriptionPlans ??= new SubscriptionPlanRepository(_db);
 
    public IDisputeRepository Disputes => _disputes ??= new DisputeRepository(_db);
    
    public IEmployeeRepository Employees => _employees ??= new EmployeeRepository(_db);
    
    public IStaffRepository Staff => _staff ??= new StaffRepository(_db);
    //SaveChanges 
 
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        UpdateTimestamps();
        return await _db.SaveChangesAsync(ct);
    }
 
    //Transactions explicites
 
    public async Task ExecuteInTransactionAsync(Func<Task> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
 
        await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                await action();
                await SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        });
    }
 
    public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken ct = default)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
 
        return await strategy.ExecuteAsync(async () =>
        {
            await using var tx = await _db.Database.BeginTransactionAsync(ct);
            try
            {
                var result = await action();
                await SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
                return result;
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
            finally
            {
                if (tx is not null) await tx.DisposeAsync();
            }
        });
    }
 
    // Helpers
 
    /// <summary>
    /// Met à jour la shadow property updated_at sur toutes les entités
    /// Added ou Modified qui en disposent.
    /// </summary>
    private void UpdateTimestamps()
    {
        var entries = _db.ChangeTracker
            .Entries()
            .Where(e => e.State is EntityState.Added or EntityState.Modified);
 
        foreach (var entry in entries)
        {
            if (entry.Metadata.FindProperty("updated_at") is not null)
                entry.Property("updated_at").CurrentValue = DateTimeOffset.UtcNow;
        }
    }
 
    public async ValueTask DisposeAsync() => await _db.DisposeAsync();
}