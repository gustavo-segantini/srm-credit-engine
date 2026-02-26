using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SrmCreditEngine.Domain.Entities;

namespace SrmCreditEngine.Infrastructure.Data.Configurations;

public sealed class SettlementConfiguration : IEntityTypeConfiguration<Settlement>
{
    public void Configure(EntityTypeBuilder<Settlement> builder)
    {
        builder.ToTable("settlements");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id");

        builder.Property(s => s.ReceivableId).HasColumnName("receivable_id").IsRequired();

        builder.HasIndex(s => s.ReceivableId).IsUnique()
            .HasDatabaseName("ix_settlements_receivable_id");

        builder.Property(s => s.FaceValue).HasColumnName("face_value").HasPrecision(18, 8).IsRequired();
        builder.Property(s => s.FaceCurrency).HasColumnName("face_currency").HasConversion<int>().IsRequired();
        builder.Property(s => s.BaseRate).HasColumnName("base_rate").HasPrecision(18, 8).IsRequired();
        builder.Property(s => s.AppliedSpread).HasColumnName("applied_spread").HasPrecision(18, 8).IsRequired();
        builder.Property(s => s.TermInMonths).HasColumnName("term_in_months").IsRequired();
        builder.Property(s => s.PresentValue).HasColumnName("present_value").HasPrecision(18, 8).IsRequired();
        builder.Property(s => s.Discount).HasColumnName("discount").HasPrecision(18, 8).IsRequired();
        builder.Property(s => s.PaymentCurrency).HasColumnName("payment_currency").HasConversion<int>().IsRequired();
        builder.Property(s => s.NetDisbursement).HasColumnName("net_disbursement").HasPrecision(18, 8).IsRequired();
        builder.Property(s => s.ExchangeRateApplied).HasColumnName("exchange_rate_applied").HasPrecision(18, 8).IsRequired();
        builder.Property(s => s.Status).HasColumnName("status").HasConversion<int>().IsRequired();
        builder.Property(s => s.SettledAt).HasColumnName("settled_at");
        builder.Property(s => s.FailureReason).HasColumnName("failure_reason").HasMaxLength(500);
        builder.Property(s => s.CreatedAt).HasColumnName("created_at");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at");

        // Optimistic concurrency token — map RowVersion to PostgreSQL xmin system column
        builder.Property(s => s.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // Analytics indexes
        builder.HasIndex(s => s.Status).HasDatabaseName("ix_settlements_status");
        builder.HasIndex(s => s.PaymentCurrency).HasDatabaseName("ix_settlements_payment_currency");
        builder.HasIndex(s => s.CreatedAt).HasDatabaseName("ix_settlements_created_at");
        builder.HasIndex(s => s.SettledAt).HasDatabaseName("ix_settlements_settled_at");
    }
}
