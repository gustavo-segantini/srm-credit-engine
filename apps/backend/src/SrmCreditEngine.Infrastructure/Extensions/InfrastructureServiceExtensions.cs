using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Application.Services;
using SrmCreditEngine.Domain.Interfaces.Repositories;
using SrmCreditEngine.Infrastructure.Analytics;
using SrmCreditEngine.Infrastructure.Data;
using SrmCreditEngine.Infrastructure.ExternalProviders;
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

        // External FX Rate Provider — HttpClient with Polly resilience pipeline
        // Pipeline: retry (3x, exponential back-off 2s/4s/8s) + circuit breaker
        services.AddHttpClient<IFxRateProviderService, FxRateProviderService>(client =>
            {
                client.BaseAddress = new Uri(
                    configuration["FxProvider:BaseUrl"] ?? "https://api.frankfurter.app/");
                client.Timeout = TimeSpan.FromSeconds(10);
                client.DefaultRequestHeaders.Add("Accept", "application/json");
            })
            .AddStandardResilienceHandler(options =>
            {
                // Retry: 3 attempts with exponential back-off
                options.Retry.MaxRetryAttempts = 3;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.Delay = TimeSpan.FromSeconds(2);

                // Circuit Breaker: opens after 50% failure rate over 30-second sampling window
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(20);

                // Total timeout per attempt
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(8);
            });

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
