using AfriPay.Domain.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class SubscriptionPlanConfiguration
    : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> b)
    {
        b.ToTable("subscription_plans");
        b.HasKey(x => x.Id);
        b.Property(x => x.MerchantId).HasColumnName("merchant_id").IsRequired();
        b.Property(x => x.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        b.Property(x => x.Description).HasColumnName("description").HasMaxLength(500);
        b.Property(x => x.Amount).HasColumnName("amount").IsRequired();
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        b.Property(x => x.Interval).HasColumnName("interval")
            .HasConversion<string>().HasMaxLength(15).IsRequired();
        b.Property(x => x.IntervalCount).HasColumnName("interval_count").IsRequired();
        b.Property(x => x.ProviderKey).HasColumnName("provider_key").HasMaxLength(30).IsRequired();
        b.Property(x => x.TrialDays).HasColumnName("trial_days").IsRequired();
        b.Property(x => x.IsActive).HasColumnName("is_active").IsRequired();
        b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        b.HasIndex(x => x.MerchantId).HasDatabaseName("idx_subscription_plans_merchant");
    }
}