using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SrmCreditEngine.Domain.Entities;

namespace SrmCreditEngine.Infrastructure.Data.Configurations;

public sealed class CedentConfiguration : IEntityTypeConfiguration<Cedent>
{
    public void Configure(EntityTypeBuilder<Cedent> builder)
    {
        builder.ToTable("cedents");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");

        builder.Property(c => c.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(c => c.Cnpj)
            .HasColumnName("cnpj")
            .HasMaxLength(14)
            .IsFixedLength()
            .IsRequired();

        builder.HasIndex(c => c.Cnpj).IsUnique()
            .HasDatabaseName("ix_cedents_cnpj");

        builder.Property(c => c.ContactEmail)
            .HasColumnName("contact_email")
            .HasMaxLength(200);

        builder.Property(c => c.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(c => c.CreatedAt).HasColumnName("created_at");
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        // Optimistic concurrency — map RowVersion to PostgreSQL xmin system column
        builder.Property(c => c.RowVersion)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        builder.HasMany(c => c.Receivables)
            .WithOne(r => r.Cedent)
            .HasForeignKey(r => r.CedentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed a default cedent for testing
        builder.HasData(new
        {
            Id = new Guid("33333333-0000-0000-0000-000000000001"),
            Name = "SRM Asset Management LTDA",
            Cnpj = "12345678000199",
            ContactEmail = "ops@srmasset.com.br",
            IsActive = true,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        });
    }
}
