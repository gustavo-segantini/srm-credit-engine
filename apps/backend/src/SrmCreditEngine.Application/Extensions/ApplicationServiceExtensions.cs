using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using SrmCreditEngine.Application.Interfaces;
using SrmCreditEngine.Application.Services;
using SrmCreditEngine.Application.Strategies;
using SrmCreditEngine.Domain.Interfaces.Strategies;

namespace SrmCreditEngine.Application.Extensions;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Register all pricing strategies
        services.AddScoped<IPricingStrategy, DuplicataMercantilPricingStrategy>();
        services.AddScoped<IPricingStrategy, ChequePredatadoPricingStrategy>();
        services.AddScoped<PricingStrategyFactory>();

        // Register application services
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<ICurrencyService, CurrencyService>();
        services.AddScoped<ISettlementService, SettlementService>();

        // Register FluentValidation validators from this assembly
        services.AddValidatorsFromAssembly(typeof(ApplicationServiceExtensions).Assembly);

        return services;
    }
}
