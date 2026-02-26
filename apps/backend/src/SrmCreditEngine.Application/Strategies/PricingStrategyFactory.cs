using SrmCreditEngine.Domain.Enums;
using SrmCreditEngine.Domain.Exceptions;
using SrmCreditEngine.Domain.Interfaces.Strategies;

namespace SrmCreditEngine.Application.Strategies;

/// <summary>
/// Factory that resolves the appropriate pricing strategy for a given receivable type.
/// Strategies are injected via DI — open/closed principle: add new types without modifying this class.
/// </summary>
public sealed class PricingStrategyFactory
{
    private readonly IReadOnlyDictionary<ReceivableType, IPricingStrategy> _strategies;

    public PricingStrategyFactory(IEnumerable<IPricingStrategy> strategies)
    {
        _strategies = strategies.ToDictionary(s => s.SupportedType);
    }

    public IPricingStrategy Resolve(ReceivableType type)
    {
        if (!_strategies.TryGetValue(type, out var strategy))
            throw new BusinessRuleViolationException(
                "STRATEGY_NOT_FOUND",
                $"No pricing strategy registered for receivable type '{type}'.");

        return strategy;
    }
}
