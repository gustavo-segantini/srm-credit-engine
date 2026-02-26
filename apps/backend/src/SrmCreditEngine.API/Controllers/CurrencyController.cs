using FluentValidation;
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
    private readonly IValidator<UpdateExchangeRateRequest> _validator;

    public CurrencyController(
        ICurrencyService currencyService,
        IValidator<UpdateExchangeRateRequest> validator)
    {
        _currencyService = currencyService;
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
}
