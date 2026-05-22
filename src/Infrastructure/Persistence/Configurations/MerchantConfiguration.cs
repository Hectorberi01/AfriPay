using AfriPay.Domain.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class MerchantConfiguration : IEntityTypeConfiguration<Merchant>
{
    public void Configure(EntityTypeBuilder<Merchant> b)
    {
        b.ToTable("merchants");
        b.HasKey(m => m.Id);
        b.Property(m => m.BusinessName).HasMaxLength(255).IsRequired();
        b.Property(m => m.Email).HasMaxLength(255).IsRequired();
        b.Property(m => m.Country).HasMaxLength(2).IsFixedLength().IsRequired();
        b.Property(m => m.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(m => m.Plan).HasColumnName("pricing_plan").HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property<DateTimeOffset?>("deleted_at");
        b.OwnsOne(m => m.WebhookConfig, wh => {
            wh.Property(w => w.Url).HasColumnName("webhook_url");
            wh.Property(w => w.Secret).HasColumnName("webhook_secret");
        });
 
        // KybInfo → table séparée merchant_kyb (one-to-one owned)
        // WithOwner() + HasKey() sont obligatoires pour les owned types en table séparée
        b.OwnsOne(m => m.KybInfo, kyb => {
            kyb.ToTable("merchant_kyb");
            kyb.WithOwner().HasForeignKey("merchant_id");
            kyb.HasKey("merchant_id");
            kyb.Property(k => k.LegalName).HasMaxLength(255).IsRequired();
            kyb.Property(k => k.RegistrationNumber).HasMaxLength(100).IsRequired();
            kyb.Property(k => k.Country).HasMaxLength(2).IsFixedLength().IsRequired();
            kyb.Property(k => k.SubmittedAt).IsRequired();
        });
 
        b.OwnsOne(m => m.Limits, l => {
            l.Property(x => x.MonthlyTransactionLimit).HasColumnName("monthly_tx_limit");
            l.Property(x => x.RatePerMinute).HasColumnName("rate_per_minute");
            l.Property(x => x.CanAccessAllProviders).HasColumnName("all_providers");
        });
        // Navigation ApiKeys → backing field _apiKeys (IReadOnlyList encapsulation DDD)
        b.Navigation(m => m.ApiKeys)
            .HasField("_apiKeys")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
 
        b.HasMany(m => m.ApiKeys)
            .WithOne()
            .HasForeignKey(k => k.MerchantId)
            .OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(m => m.Email).IsUnique();
 
        // Auth
        b.Property(m => m.PasswordHash)
            .HasColumnName("password_hash")
            .HasMaxLength(100);
 
        b.Property(m => m.RefreshToken)
            .HasColumnName("refresh_token")
            .HasMaxLength(200);
 
        b.Property(m => m.RefreshTokenExpiresAt)
            .HasColumnName("refresh_token_expires_at");
 
        b.HasIndex(m => m.RefreshToken)
            .HasFilter("refresh_token IS NOT NULL")
            .HasDatabaseName("idx_merchants_refresh_token");
        b.HasQueryFilter(m => EF.Property<DateTimeOffset?>(m, "deleted_at") == null);
    }
}