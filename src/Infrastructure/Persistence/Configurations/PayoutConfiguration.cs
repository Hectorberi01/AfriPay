using AfriPay.Domain.Payouts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> b)
    {
        b.ToTable("payouts");
        b.HasKey(x => x.Id);
 
        b.Property(x => x.MerchantId).HasColumnName("merchant_id").IsRequired();
        b.Property(x => x.Status).HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Method).HasColumnName("method")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Amount).HasColumnName("amount").IsRequired();
        b.Property(x => x.Currency).HasColumnName("currency").HasMaxLength(3).IsRequired();
        b.Property(x => x.ProviderReference).HasColumnName("provider_reference").HasMaxLength(100);
        b.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
        b.Property(x => x.Schedule).HasColumnName("schedule")
            .HasConversion<string>().HasMaxLength(20).IsRequired();
        b.Property(x => x.Notes).HasColumnName("notes").HasMaxLength(500);
        b.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
        b.Property(x => x.UpdatedAt).HasColumnName("updated_at").IsRequired();
        b.Property(x => x.ProcessingAt).HasColumnName("processing_at");
        b.Property(x => x.CompletedAt).HasColumnName("completed_at");
        b.Property(x => x.FailedAt).HasColumnName("failed_at");
 
        // Destination owned type
        b.OwnsOne(x => x.Destination, d =>
        {
            d.Property(x => x.Method).HasColumnName("dest_method")
                .HasConversion<string>().HasMaxLength(20);
            d.Property(x => x.PhoneNumber).HasColumnName("dest_phone").HasMaxLength(20);
            d.Property(x => x.ProviderKey).HasColumnName("dest_provider").HasMaxLength(30);
 
            d.OwnsOne(x => x.BankAccount, ba =>
            {
                ba.Property(x => x.Iban).HasColumnName("dest_iban").HasMaxLength(34);
                ba.Property(x => x.Bic).HasColumnName("dest_bic").HasMaxLength(11);
                ba.Property(x => x.AccountHolder).HasColumnName("dest_account_holder").HasMaxLength(255);
                ba.Property(x => x.BankName).HasColumnName("dest_bank_name").HasMaxLength(255);
                ba.Property(x => x.Country).HasColumnName("dest_bank_country").HasMaxLength(2);
            });
        });
 
        b.HasIndex(x => x.MerchantId).HasDatabaseName("idx_payouts_merchant");
        b.HasIndex(x => x.Status).HasFilter("status = 'Pending'")
            .HasDatabaseName("idx_payouts_pending");
    }
}