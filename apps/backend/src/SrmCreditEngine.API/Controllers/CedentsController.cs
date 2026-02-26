using Microsoft.AspNetCore.Mvc;
using SrmCreditEngine.Domain.Interfaces.Repositories;

namespace SrmCreditEngine.API.Controllers;

[ApiController]
[Route("api/v1/cedents")]
[Produces("application/json")]
public sealed class CedentsController : ControllerBase
{
    private readonly ICedentRepository _cedentRepository;

    public CedentsController(ICedentRepository cedentRepository)
    {
        _cedentRepository = cedentRepository;
    }

    /// <summary>Returns all active cedents.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var cedents = await _cedentRepository.GetAllActiveAsync(cancellationToken);
        return Ok(cedents.Select(c => new
        {
            c.Id,
            c.Name,
            c.Cnpj,
            c.ContactEmail,
            c.IsActive,
            c.CreatedAt
        }));
    }

    /// <summary>Returns a cedent by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById([FromRoute] Guid id, CancellationToken cancellationToken)
    {
        var cedent = await _cedentRepository.GetByIdAsync(id, cancellationToken);
        if (cedent == null) return NotFound();
        return Ok(new
        {
            cedent.Id,
            cedent.Name,
            cedent.Cnpj,
            cedent.ContactEmail,
            cedent.IsActive,
            cedent.CreatedAt
        });
    }
}
