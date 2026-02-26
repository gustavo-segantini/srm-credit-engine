using Microsoft.EntityFrameworkCore;
using SrmCreditEngine.Domain.Entities;

namespace SrmCreditEngine.Infrastructure.Data;

public sealed class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Currency> Currencies => Set<Currency>();
    public DbSet<ExchangeRate> ExchangeRates => Set<ExchangeRate>();
    public DbSet<Cedent> Cedents => Set<Cedent>();
    public DbSet<Receivable> Receivables => Set<Receivable>();
    public DbSet<Settlement> Settlements => Set<Settlement>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all entity configurations from this assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Default schema
        modelBuilder.HasDefaultSchema("credit");

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Always use decimal with precision 18,8 for financial data
        configurationBuilder.Properties<decimal>()
            .HavePrecision(18, 8);

        // All DateTime fields stored as UTC in PostgreSQL (timestamptz)
        configurationBuilder.Properties<DateTime>()
            .HaveColumnType("timestamptz");

        configurationBuilder.Properties<DateTime?>()
            .HaveColumnType("timestamptz");
    }
}
