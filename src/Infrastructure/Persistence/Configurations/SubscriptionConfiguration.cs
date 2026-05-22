using AfriPay.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionConfiguration
    : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> b)
    {
        b.ToTable("subscriptions");
        b.HasKey(x => x.Id);
        b.Property(x => x.MerchantId).HasColumnName("merchant_id").IsRequired();
        b.Property(x => x.PlanId).HasColumnName("plan_id").IsRequired();
        b.Property(x => x.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.CustomerPhone).HasColumnName("customer_phone").HasMaxLength(20).IsRequired();
        b.Property(x => x.CustomerEmail).HasColumnName("customer_email").HasMaxLength(255);
        b.Property(x => x.CustomerName).HasColumnName("customer_name").HasMaxLength(255);
        b.Property(x => x.Amount).HasColumnName("amount").IsRequired();
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        b.Property(x => x.ProviderKey).HasColumnName("provider_key").HasMaxLength(30).IsRequired();
        b.Property(x => x.CurrentPeriodStart).HasColumnName("current_period_start").IsRequired();
        b.Property(x => x.CurrentPeriodEnd).HasColumnName("current_period_end").IsRequired();
        b.Property(x => x.TrialEnd).HasColumnName("trial_end");
        b.Property(x => x.CancelledAt).HasColumnName("cancelled_at");
        b.Property(x => x.EndsAt).HasColumnName("ends_at");
        b.Property(x => x.RetryCount).HasColumnName("retry_count").IsRequired();
        b.Property(x => x.NextRetryAt).HasColumnName("next_retry_at");
        b.Property(x => x.LastFailReason).HasColumnName("last_fail_reason").HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        b.HasIndex(x => x.MerchantId).HasDatabaseName("idx_subscriptions_merchant");
        b.HasIndex(x => new { x.Status, x.CurrentPeriodEnd })
            .HasDatabaseName("idx_subscriptions_renewal");
    }
}