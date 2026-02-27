using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.Services;

namespace SrmCreditEngine.API.Controllers;

[ApiController]
[Route("api/v1/cedents")]
[Produces("application/json")]
public sealed class CedentsController : ControllerBase
{
    private readonly ICedentService _cedentService;
    private readonly IValidator<CreateCedentRequest> _createValidator;
    private readonly IValidator<UpdateCedentRequest> _updateValidator;

    public CedentsController(
        ICedentService cedentService,
        IValidator<CreateCedentRequest> createValidator,
        IValidator<UpdateCedentRequest> updateValidator)
    {
        _cedentService = cedentService;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    /// <summary>Returns all active cedents.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var cedents = await _cedentService.GetAllActiveAsync(cancellationToken);
        return Ok(cedents);
    }

    /// <summary>Returns a cedent by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var cedent = await _cedentService.GetByIdAsync(id, cancellationToken);
        if (cedent is null) return NotFound();
        return Ok(cedent);
    }

    /// <summary>Onboards a new cedent. CNPJ must be unique.</summary>
    [Authorize]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateCedentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await _cedentService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>Updates a cedent's name and contact email.</summary>
    [Authorize]
    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateCedentRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await _updateValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));

        var result = await _cedentService.UpdateAsync(id, request, cancellationToken);
        return Ok(result);
    }

    /// <summary>Soft-deletes (deactivates) a cedent. Idempotent.</summary>
    [Authorize]
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Deactivate([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        await _cedentService.DeactivateAsync(id, cancellationToken);
        return NoContent();
    }
}
