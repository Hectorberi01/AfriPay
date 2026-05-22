using AfriPay.Domain.Merchants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

/// <summary>
/// Configuration EF Core pour l'entité ApiKey.
///
/// Contraintes métier mappées :
///   - KeyHash : CHAR(64) UNIQUE — SHA-256 hex, indexé pour le lookup auth (chemin chaud)
///   - Index partiel sur is_active = true — seules les clés actives sont cherchées
///   - Type : string enum (Live | Sandbox)
///   - Soft revocation via RevokedAt nullable
/// </summary>
public sealed class ApiKeyConfiguration : IEntityTypeConfiguration<ApiKey>
{
    public void Configure(EntityTypeBuilder<ApiKey> b)
    {
        b.ToTable("api_keys");
        b.HasKey(k => k.Id);

        // ── Clé API ────────────────────────────────────────────
        b.Property(k => k.KeyHash)
            .HasColumnName("key_hash")
            .HasMaxLength(64)
            // VARCHAR(64) — pas CHAR(64) : PostgreSQL CHAR complète avec des espaces,
            // ce qui fait échouer la comparaison == même avec la valeur correcte.
            .IsRequired();

        b.Property(k => k.Prefix)
            .HasColumnName("key_prefix")
            .HasMaxLength(20)
            .IsRequired();

        // ── Type enum → string ─────────────────────────────────
        b.Property(k => k.Type)
            .HasColumnName("key_type")
            .HasConversion<string>()
            .HasMaxLength(10)
            .IsRequired();

        // ── Statut ─────────────────────────────────────────────
        b.Property(k => k.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        // ── Relation avec Merchant ─────────────────────────────
        b.Property(k => k.MerchantId)
            .HasColumnName("merchant_id")
            .IsRequired();

        // ── Timestamps ─────────────────────────────────────────
        b.Property(k => k.CreatedAt)
            .HasColumnName("created_at")
            .IsRequired();

        b.Property(k => k.RevokedAt)
            .HasColumnName("revoked_at");

        // ── Index ──────────────────────────────────────────────

        // Index principal : lookup auth à chaque requête HTTP
        // Partiel sur is_active = true — ignore les clés révoquées
        b.HasIndex(k => k.KeyHash)
            .IsUnique()
            .HasFilter("is_active = true")
            .HasDatabaseName("idx_api_keys_hash_active");

        // Index secondaire : lister les clés d'un marchand
        b.HasIndex(k => k.MerchantId)
            .HasDatabaseName("idx_api_keys_merchant_id");
    }
}