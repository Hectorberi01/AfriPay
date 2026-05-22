using AfriPay.Domain.Currency;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AfriPay.Infrastructure.Persistence.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> b)
    {
        b.ToTable("exchange_rates");
        b.HasKey(e => e.Id);
 
        b.Property(e => e.OfficialRate)
            .HasColumnType("numeric(18,8)")
            .IsRequired();
 
        b.Property(e => e.Spread)
            .HasColumnType("numeric(6,5)")
            .IsRequired();
 
        b.Property(e => e.EffectiveRate)
            .HasColumnType("numeric(18,8)")
            .IsRequired();
 
        b.Property(e => e.Source)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();
 
        b.Property(e => e.RecordedAt).IsRequired();
        b.Property(e => e.ValidUntil).IsRequired();
 
        // L'index sur les colonnes du owned type CurrencyPair se déclare
        // à l'intérieur du OwnsOne — EF Core résout alors les colonnes
        // "from_currency" et "to_currency" correctement.
        b.OwnsOne(e => e.Pair, pair =>
        {
            pair.Property(p => p.From)
                .HasColumnName("from_currency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
 
            pair.Property(p => p.To)
                .HasColumnName("to_currency")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();
 
            // Index composite : (from_currency, to_currency) ASC + recorded_at DESC
            // Optimise le lookup GetLatestValidAsync(pair) et GetAtInstantAsync(pair, at)
            pair.HasIndex(p => new { p.From, p.To });
        });
 
        // Index séparé sur recorded_at pour les requêtes d'historique
        b.HasIndex(e => e.RecordedAt)
            .IsDescending();
 
        b.ToTable(t => t.HasComment(
            "Append-only. Never UPDATE. Insert new row for each rate refresh."));
    }
}