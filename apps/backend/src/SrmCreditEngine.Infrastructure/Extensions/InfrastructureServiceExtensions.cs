using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Infrastructure.Analytics;
using SrmCreditEngine.Infrastructure.Data;
using SrmCreditEngine.Infrastructure.Repositories;

namespace SrmCreditEngine.Infrastructure.Extensions;

public static class InfrastructureServiceExtensions
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database
        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                npgsql =>
                {
                    npgsql.MigrationsHistoryTable("__ef_migrations_history", "credit");
                    npgsql.EnableRetryOnFailure(
                        maxRetryCount: 5,
                        maxRetryDelay: TimeSpan.FromSeconds(30),
                        errorCodesToAdd: null);
                })
            .EnableSensitiveDataLogging(false)
            .UseSnakeCaseNamingConvention());

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories
        services.AddScoped<ICurrencyRepository, CurrencyRepository>();
        services.AddScoped<IExchangeRateRepository, ExchangeRateRepository>();
        services.AddScoped<ICedentRepository, CedentRepository>();
        services.AddScoped<IReceivableRepository, ReceivableRepository>();
        services.AddScoped<ISettlementRepository, SettlementRepository>();

        // Analytics (Dapper-based)
        services.AddScoped<ISettlementStatementQuery, SettlementStatementQuery>();

        return services;
    }

    /// <summary>
    /// Applies pending EF Core migrations on startup.
    /// Safe for containerized environments.
    /// </summary>
    public static async Task ApplyMigrationsAsync(this IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.MigrateAsync();
    }
}
