using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SrmCreditEngine.Domain.Entities;

namespace SrmCreditEngine.Infrastructure.Data.Configurations;

public sealed class ReceivableConfiguration : IEntityTypeConfiguration<Receivable>
{
    public void Configure(EntityTypeBuilder<Receivable> builder)
    {
        builder.ToTable("receivables");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");

        builder.Property(r => r.CedentId).HasColumnName("cedent_id").IsRequired();

        builder.Property(r => r.DocumentNumber)
            .HasColumnName("document_number")
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(r => new { r.DocumentNumber, r.CedentId }).IsUnique()
            .HasDatabaseName("ix_receivables_doc_cedent");

        builder.Property(r => r.Type)
            .HasColumnName("type")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.FaceValue)
            .HasColumnName("face_value")
            .HasPrecision(18, 8)
            .IsRequired();

        builder.Property(r => r.FaceCurrency)
            .HasColumnName("face_currency")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(r => r.DueDate).HasColumnName("due_date").IsRequired();
        builder.Property(r => r.SubmittedAt).HasColumnName("submitted_at").IsRequired();
        builder.Property(r => r.CreatedAt).HasColumnName("created_at");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at");

        // Index for analytics queries
        builder.HasIndex(r => r.CedentId).HasDatabaseName("ix_receivables_cedent_id");
        builder.HasIndex(r => r.DueDate).HasDatabaseName("ix_receivables_due_date");
    }
}
