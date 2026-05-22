using AfriPay.Domain.Payments;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("payments");
        b.HasKey(p => p.Id);
        b.Property(p => p.IdempotencyKey).HasMaxLength(255).IsRequired();
        b.Property(p => p.ProviderKey).HasMaxLength(30).IsRequired();
        b.Property(p => p.ProviderReference).HasMaxLength(255);
        b.Property(p => p.ProviderUsed).HasMaxLength(30);
        b.Property(p => p.Status).HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(p => p.UssdCode).HasColumnName("ussd_code").HasMaxLength(50);
        // Chaque Money value object a ses propres colonnes dédiées.
        // Partager "currency" entre Amount, Fee et Net cause l'erreur
        // "configured with different column nullability settings".
        b.OwnsOne(p => p.Amount, m => {
            m.Property(x => x.Amount).HasColumnName("amount").IsRequired();
            m.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsFixedLength().IsRequired();
        });
        
        b.OwnsOne(p => p.Fee, m =>
        {
            m.Property(x => x.Amount).HasColumnName("fee_amount");
            m.Property(x => x.Currency).HasColumnName("fee_currency").HasMaxLength(3);
        });
        
        b.OwnsOne(p => p.Net, m =>
        {
            m.Property(x => x.Amount).HasColumnName("net_amount");
            m.Property(x => x.Currency).HasColumnName("net_currency").HasMaxLength(3);
        });
        
        b.OwnsOne(p => p.Customer, c => {
            c.Property(x => x.PhoneNumber).HasColumnName("customer_phone").HasMaxLength(20);
            c.Property(x => x.Email).HasColumnName("customer_email").HasMaxLength(255);
            c.Property(x => x.Name).HasColumnName("customer_name").HasMaxLength(255);
        });
        b.Property(p => p.Metadata).HasColumnType("jsonb")
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<Dictionary<string,string>>(v, (System.Text.Json.JsonSerializerOptions?)null)!);
        b.OwnsMany(p => p.Transitions, t => {
            t.ToTable("payment_status_transitions");
            t.HasKey(x => x.Id);
            t.Property(x => x.FromStatus).HasConversion<string>().HasMaxLength(20);
            t.Property(x => x.ToStatus).HasConversion<string>().HasMaxLength(20).IsRequired();
            t.WithOwner().HasForeignKey(x => x.PaymentId);
        });
        b.OwnsMany(p => p.Attempts, a => {
            a.ToTable("provider_attempts");
            a.HasKey(x => x.Id);
            a.WithOwner().HasForeignKey(x => x.PaymentId);
        });
        b.HasIndex(p => p.Status).HasFilter("status = 'Pending'");
        b.HasIndex(p => new { p.MerchantId, p.IdempotencyKey }).IsUnique();
    }
}