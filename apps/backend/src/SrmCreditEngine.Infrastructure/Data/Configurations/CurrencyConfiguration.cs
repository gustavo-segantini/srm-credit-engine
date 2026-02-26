using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SrmCreditEngine.Domain.Entities;
using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.Infrastructure.Data.Configurations;

public sealed class CurrencyConfiguration : IEntityTypeConfiguration<Currency>
{
    public void Configure(EntityTypeBuilder<Currency> builder)
    {
        builder.ToTable("currencies");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.Code)
            .HasColumnName("code")
            .HasConversion<int>()
            .IsRequired();

        builder.HasIndex(c => c.Code).IsUnique();

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.Symbol)
            .HasColumnName("symbol")
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(c => c.DecimalPlaces)
            .HasColumnName("decimal_places")
            .IsRequired();

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        // Navigation properties
        builder.HasMany(c => c.ExchangeRatesFrom)
            .WithOne(e => e.FromCurrency)
            .HasForeignKey(e => e.FromCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.ExchangeRatesTo)
            .WithOne(e => e.ToCurrency)
            .HasForeignKey(e => e.ToCurrencyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed data
        builder.HasData(
            new { Id = new Guid("11111111-0000-0000-0000-000000000001"), Code = CurrencyCode.BRL, Name = "Real Brasileiro", Symbol = "R$", DecimalPlaces = 2, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
            new { Id = new Guid("11111111-0000-0000-0000-000000000002"), Code = CurrencyCode.USD, Name = "US Dollar", Symbol = "$", DecimalPlaces = 2, IsActive = true, CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc) }
        );
    }
}
