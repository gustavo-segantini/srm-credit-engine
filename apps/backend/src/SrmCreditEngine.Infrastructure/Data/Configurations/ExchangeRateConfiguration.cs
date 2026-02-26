using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SrmCreditEngine.Domain.Entities;

namespace SrmCreditEngine.Infrastructure.Data.Configurations;

public sealed class ExchangeRateConfiguration : IEntityTypeConfiguration<ExchangeRate>
{
    public void Configure(EntityTypeBuilder<ExchangeRate> builder)
    {
        builder.ToTable("exchange_rates");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id");

        builder.Property(e => e.FromCurrencyId).HasColumnName("from_currency_id").IsRequired();
        builder.Property(e => e.ToCurrencyId).HasColumnName("to_currency_id").IsRequired();

        builder.Property(e => e.Rate)
            .HasColumnName("rate")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(e => e.EffectiveDate).HasColumnName("effective_date").IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at");

        builder.Property(e => e.Source)
            .HasColumnName("source")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(e => e.CreatedAt).HasColumnName("created_at");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at");

        // Optimistic concurrency — map RowVersion to PostgreSQL xmin system column
        builder.Property(e => e.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Index for fast lookup of latest rate
        builder.HasIndex(e => new { e.FromCurrencyId, e.ToCurrencyId, e.EffectiveDate })
            .HasDatabaseName("ix_exchange_rates_pair_date");

        // Seed default USD→BRL rate
        builder.HasData(new
        {
            Id = new Guid("22222222-0000-0000-0000-000000000001"),
            FromCurrencyId = new Guid("11111111-0000-0000-0000-000000000002"), // USD
            ToCurrencyId = new Guid("11111111-0000-0000-0000-000000000001"),   // BRL
            Rate = 5.75m,
            EffectiveDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            ExpiresAt = (DateTime?)null,
            Source = "SEED",
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
