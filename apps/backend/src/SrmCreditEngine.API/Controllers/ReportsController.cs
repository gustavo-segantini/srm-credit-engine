using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.Services;
using SrmCreditEngine.Domain.Enums;

namespace SrmCreditEngine.API.Controllers;

[ApiController]
[Route("api/v1/reports")]
[Produces("application/json")]
public sealed class ReportsController : ControllerBase
{
    private readonly ISettlementService _settlementService;

    public ReportsController(ISettlementService settlementService)
    {
        _settlementService = settlementService;
    }

    /// <summary>
    /// Returns a paginated settlement statement filtered by period, cedent and currency.
    /// Powered by Dapper with raw SQL for high-performance analytics.
    /// </summary>
    [Authorize]
    [HttpGet("settlement-statement")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettlementStatement(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] Guid? cedentId,
        [FromQuery] CurrencyCode? paymentCurrency,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 50;

        var request = new GetSettlementStatementRequest(
            From: from,
            To: to,
            CedentId: cedentId,
            PaymentCurrency: paymentCurrency,
            Page: page,
            PageSize: pageSize);

        var result = await _settlementService.GetStatementAsync(request, cancellationToken);
        return Ok(result);
    }
}
