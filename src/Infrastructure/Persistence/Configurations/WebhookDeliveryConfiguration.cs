using AfriPay.Domain.Webhooks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> b)
    {
        b.ToTable("webhook_deliveries");
        b.HasKey(w => w.Id);

        b.Property(w => w.MerchantId).IsRequired();
        b.Property(w => w.EventType).HasMaxLength(50).IsRequired();
        b.Property(w => w.Payload).HasColumnType("jsonb").IsRequired();
        b.Property(w => w.Signature).HasMaxLength(100).IsRequired();
        b.Property(w => w.TargetUrl).IsRequired();
        b.Property(w => w.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();
        b.Property(w => w.AttemptCount).IsRequired();
        b.Property(w => w.NextRetryAt);
        b.Property(w => w.DeliveredAt);
        b.Property(w => w.CreatedAt).IsRequired();

        // Backing field pour la collection Attempts
        // WebhookDelivery.Attempts est IReadOnlyList<DeliveryAttempt>
        // exposant le champ privé _attempts.
        // HasField() indique à EF quel champ utiliser en lecture/écriture.
        b.Navigation(w => w.Attempts)
            .HasField("_attempts")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        b.OwnsMany(w => w.Attempts, a =>
        {
            a.ToTable("webhook_delivery_attempts");
            a.HasKey(x => x.Id);
            a.Property(x => x.AttemptNumber).IsRequired();
            a.Property(x => x.HttpStatusCode);
            a.Property(x => x.ResponseBody).HasMaxLength(500);
            a.Property(x => x.LatencyMs);
            a.Property(x => x.Success).IsRequired();
            a.Property(x => x.FailureReason).HasMaxLength(500);
            a.Property(x => x.AttemptedAt).IsRequired();
            a.WithOwner().HasForeignKey(x => x.DeliveryId);
        });

        b.HasIndex(w => w.MerchantId);
        b.HasIndex(w => w.EventType);
        b.HasIndex(w => w.NextRetryAt)
            .HasFilter("status IN ('pending','retrying')");
    }
}
