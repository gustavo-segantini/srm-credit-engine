using Microsoft.AspNetCore.Mvc;
using SrmCreditEngine.Domain.Interfaces.Repositories;

namespace SrmCreditEngine.API.Controllers;

[ApiController]
[Route("api/v1/receivables")]
[Produces("application/json")]
public sealed class ReceivablesController(IReceivableRepository receivableRepository) : ControllerBase
{

    /// <summary>Returns all receivables submitted by a specific cedent.</summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetByCedent(
        [FromQuery] Guid cedentId,
        CancellationToken cancellationToken)
    {
        if (cedentId == Guid.Empty)
        {
            return BadRequest(new { message = "cedentId is required." });
        }

        var receivables = await receivableRepository.GetByCedentAsync(cedentId, cancellationToken);

        return Ok(receivables.Select(r => new
        {
            r.Id,
            r.DocumentNumber,
            Type = r.Type.ToString(),
            r.FaceValue,
            FaceCurrency = r.FaceCurrency.ToString(),
            r.DueDate,
            r.SubmittedAt,
            HasSettlement = r.Settlement is not null
        }));
    }

    /// <summary>Returns a single receivable by ID.</summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        var receivable = await receivableRepository.GetByIdAsync(id, cancellationToken);
        if (receivable is null)
        {
            return NotFound();
        }

        return Ok(new
        {
            receivable.Id,
            receivable.CedentId,
            receivable.DocumentNumber,
            Type = receivable.Type.ToString(),
            receivable.FaceValue,
            FaceCurrency = receivable.FaceCurrency.ToString(),
            receivable.DueDate,
            receivable.SubmittedAt,
            HasSettlement = receivable.Settlement is not null
        });
    }
}
