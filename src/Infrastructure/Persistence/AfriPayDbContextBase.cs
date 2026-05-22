using AfriPay.Domain.Balance;
using AfriPay.Domain.Currency;
using AfriPay.Domain.Disputes;
using AfriPay.Domain.Fee;
using AfriPay.Domain.Kyb;
using AfriPay.Domain.Merchants;
using AfriPay.Domain.Payments;
using AfriPay.Domain.Refunds;
using AfriPay.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;

namespace AfriPay.Infrastructure.Persistence;

public abstract class AfriPayDbContextBase(DbContextOptions options) : DbContext(options)
{
    public DbSet<Merchant>        Merchants         => Set<Merchant>();
    public DbSet<ApiKey>          ApiKeys           => Set<ApiKey>();
    public DbSet<Payment>         Payments          => Set<Payment>();
    public DbSet<Refund>          Refunds           => Set<Refund>();
    public DbSet<WebhookDelivery> WebhookDeliveries => Set<WebhookDelivery>();
    public DbSet<ExchangeRate>    ExchangeRates     => Set<ExchangeRate>();
    
    // Phase 2
    public DbSet<MerchantBalance> MerchantBalances  => Set<MerchantBalance>();
    public DbSet<BalanceEntry>    BalanceEntries    => Set<BalanceEntry>();
    public DbSet<FeeRule>         FeeRules          => Set<FeeRule>();
    public DbSet<KybApplication>  KybApplications   => Set<KybApplication>();
    public DbSet<KybDocument>     KybDocuments      => Set<KybDocument>();
    
    public DbSet<Dispute> Disputes => Set<Dispute>();
 
    protected override void OnModelCreating(ModelBuilder mb)
    {
        mb.ApplyConfigurationsFromAssembly(typeof(AfriPayDbContextBase).Assembly);
        base.OnModelCreating(mb);
    }
}

/// <summary>Base de données production — afripay_live.</summary>
public sealed class LiveDbContext(DbContextOptions<LiveDbContext> options)
    : AfriPayDbContextBase(options);
 
/// <summary>Base de données sandbox — afripay_sandbox.</summary>
public sealed class SandboxDbContext(DbContextOptions<SandboxDbContext> options)
    : AfriPayDbContextBase(options);
    
    
public interface IDbContextFactory
{
    AfriPayDbContextBase Resolve(bool isLive);
}
 
public sealed class AfriPayDbContextFactory(LiveDbContext live, SandboxDbContext sandbox) : IDbContextFactory
{
    public AfriPayDbContextBase Resolve(bool isLive)
        => isLive ? live : sandbox;
}