using AfriPay.Domain.Audit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class AuditEntryConfiguration : IEntityTypeConfiguration<AuditEntry>
{
    public void Configure(EntityTypeBuilder<AuditEntry> b)
    {
        b.ToTable("audit_entries");
        b.HasKey(x => x.Id);
        b.ToTable(t => t.HasComment("Append-only. Never UPDATE or DELETE."));
 
        b.Property(x => x.MerchantId).HasColumnName("merchant_id").IsRequired();
        b.Property(x => x.Action).HasColumnName("action")
            .HasConversion<string>().HasMaxLength(50).IsRequired();
        b.Property(x => x.EntityType).HasColumnName("entity_type").HasMaxLength(50).IsRequired();
        b.Property(x => x.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        b.Property(x => x.ActorId).HasColumnName("actor_id").HasMaxLength(100);
        b.Property(x => x.ActorType).HasColumnName("actor_type").HasMaxLength(20);
        b.Property(x => x.IpAddress).HasColumnName("ip_address").HasMaxLength(45);
        b.Property(x => x.UserAgent).HasColumnName("user_agent").HasMaxLength(500);
        b.Property(x => x.Metadata).HasColumnName("metadata");
        b.Property(x => x.OccurredAt).HasColumnName("occurred_at").IsRequired();
 
        b.HasIndex(x => new { x.MerchantId, x.OccurredAt })
            .IsDescending(false, true)
            .HasDatabaseName("idx_audit_entries_merchant_date");
        b.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("idx_audit_entries_entity");
    }
}