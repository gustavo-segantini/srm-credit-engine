using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.Services;
using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.API.Controllers;

[ApiController]
[Route("api/v1/currency")]
[Produces("application/json")]
public sealed class CurrencyController : ControllerBase
{
    private readonly ICurrencyService _currencyService;
    private readonly IFxRateProviderService _fxRateProvider;
    private readonly IValidator<UpdateExchangeRateRequest> _validator;

    public CurrencyController(
        ICurrencyService currencyService,
        IFxRateProviderService fxRateProvider,
        IValidator<UpdateExchangeRateRequest> validator)
    {
        _currencyService = currencyService;
        _fxRateProvider = fxRateProvider;
        _validator = validator;
    }

    /// <summary>Gets the latest exchange rate for a currency pair.</summary>
    [HttpGet("exchange-rates/{from}/{to}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLatestRate(
        [FromRoute] CurrencyCode from,
        [FromRoute] CurrencyCode to,
        CancellationToken cancellationToken)
    {
        var rate = await _currencyService.GetLatestExchangeRateAsync(from, to, cancellationToken);
        return Ok(rate);
    }

    /// <summary>Creates or updates the exchange rate for a currency pair.</summary>
    [Authorize]
    [HttpPut("exchange-rates")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpsertRate(
        [FromBody] UpdateExchangeRateRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await _currencyService.UpsertExchangeRateAsync(request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Returns exchange rate history for a currency pair.</summary>
    [HttpGet("exchange-rates/{from}/{to}/history")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRateHistory(
        [FromRoute] CurrencyCode from,
        [FromRoute] CurrencyCode to,
        [FromQuery] DateTime? fromDate,
        [FromQuery] DateTime? toDate,
        CancellationToken cancellationToken)
    {
        var rates = await _currencyService.GetRateHistoryAsync(
            from, to,
            fromDate ?? DateTime.UtcNow.AddMonths(-3),
            toDate ?? DateTime.UtcNow,
            cancellationToken);

        return Ok(rates);
    }

    /// <summary>
    /// Syncs exchange rates from the external FX provider (e.g. Frankfurter API).
    /// Resilient via Polly: 3 retries with exponential back-off + circuit breaker.
    /// Falls back gracefully when the external provider is unavailable.
    /// </summary>
    [Authorize]
    [HttpPost("exchange-rates/sync")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> SyncFromProvider(
        [FromQuery] CurrencyCode from = CurrencyCode.BRL,
        [FromQuery] CurrencyCode to = CurrencyCode.USD,
        CancellationToken cancellationToken = default)
    {
        var providerResult = await _fxRateProvider.FetchRateAsync(from, to, cancellationToken);

        if (providerResult is null)
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "External FX provider unavailable. Manual rates remain active." });

        var upsertRequest = new UpdateExchangeRateRequest(
            from,
            to,
            providerResult.Rate,
            providerResult.Source);

        var result = await _currencyService.UpsertExchangeRateAsync(upsertRequest, cancellationToken);

        return Ok(new
        {
            message = "Exchange rate synced from external provider.",
            rate = result,
            syncedAt = providerResult.FetchedAt
        });
    }
}
