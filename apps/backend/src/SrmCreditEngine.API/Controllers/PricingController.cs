using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SrmCreditEngine.Application.DTOs.Requests;
using SrmCreditEngine.Application.Services;

namespace SrmCreditEngine.API.Controllers;

[ApiController]
[Route("api/v1/pricing")]
[Produces("application/json")]
public sealed class PricingController(
    IPricingService pricingService,
    IValidator<SimulatePricingRequest> validator) : ControllerBase
{

    /// <summary>
    /// Simulates the pricing of a receivable without persisting.
    /// Used by the operator panel for real-time preview.
    /// </summary>
    [Authorize]
    [EnableRateLimiting("pricing")]
    [HttpPost("simulate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Simulate(
        [FromBody] SimulatePricingRequest request,
        CancellationToken cancellationToken)
    {
        var validation = await validator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(validation.Errors.Select(e => new { e.PropertyName, e.ErrorMessage }));
        }

        var result = await pricingService.SimulateAsync(request, cancellationToken);
        return Ok(result);
    }
}
