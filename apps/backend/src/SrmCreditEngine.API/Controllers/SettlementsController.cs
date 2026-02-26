using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.Services;

namespace SrmCreditEngine.API.Controllers;

[ApiController]
[Route("api/v1/settlements")]
[Produces("application/json")]
public sealed class SettlementsController : ControllerBase
{
    private readonly ISettlementService _settlementService;
    private readonly IValidator<CreateSettlementRequest> _validator;

    public SettlementsController(
        ISettlementService settlementService,
        IValidator<CreateSettlementRequest> validator)
    {
        _settlementService = settlementService;
        _validator = validator;
    }

    /// <summary>
    /// Prices and settles a receivable in a single atomic operation.
    /// Idempotent per (cedentId, documentNumber) pair.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSettlementRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await _settlementService.CreateAndSettleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Returns a settlement by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var result = await _settlementService.GetByIdAsync(id, cancellationToken);
        return Ok(result);
    }
}
