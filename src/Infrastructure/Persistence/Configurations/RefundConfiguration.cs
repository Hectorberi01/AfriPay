using AfriPay.Domain.Refunds;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;


public sealed class RefundConfiguration : IEntityTypeConfiguration<Refund>
{
    public void Configure(EntityTypeBuilder<Refund> b)
    {
        b.ToTable("refunds");
        b.HasKey(r => r.Id);

        b.Property(r => r.PaymentId).IsRequired();
        b.Property(r => r.MerchantId).IsRequired();

        // ── Money value object → colonnes aplaties ─────────────
        b.OwnsOne(r => r.Amount, m =>
        {
            m.Property(x => x.Amount)
                .HasColumnName("amount")
                .IsRequired();
            m.Property(x => x.Currency)
                .HasColumnName("currency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
        });

        b.Property(r => r.IsPartial).IsRequired();
        
        b.Property(r => r.Status)
            .HasColumnName("status")
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
        b.Property(r => r.Reason)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();
        b.Property(r => r.Notes).HasMaxLength(1000);

        b.Property(r => r.ProviderKey).HasMaxLength(30);
        b.Property(r => r.ProviderReference).HasMaxLength(255);

        b.Property(r => r.CreatedAt).IsRequired();
        b.Property(r => r.EstimatedArrival).IsRequired();
        b.Property(r => r.UpdatedAt).IsRequired();
        b.Property(r => r.CompletedAt);
        b.Property(r => r.FailedAt);

        b.HasIndex(r => r.PaymentId);
        b.HasIndex(r => new { r.MerchantId, r.CreatedAt });
        b.HasIndex(r => r.Status).HasFilter("status = 'Pending'");
    }
}