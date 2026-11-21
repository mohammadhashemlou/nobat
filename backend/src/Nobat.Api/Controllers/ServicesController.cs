using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nobat.Application.Modules.Services.Commands;
using Nobat.Application.Modules.Services.DTOs;
using Nobat.Application.Modules.Services.Queries;

namespace Nobat.Api.Controllers;

[ApiController]
[Route("api/v1/services")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class ServicesController : ControllerBase
{
    private readonly IMediator _mediator;

    public ServicesController(IMediator mediator) => _mediator = mediator;

    /// <summary>Services of a salon.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<ServiceDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid salonId, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetServicesQuery(salonId, includeInactive), ct);
        return Ok(result);
    }

    /// <summary>Creates a service.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateServiceBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateServiceCommand
        {
            SalonId = body.SalonId,
            Name = body.Name,
            Description = body.Description,
            DurationMinutes = body.DurationMinutes,
            Price = body.Price
        }, ct);

        return CreatedAtAction(nameof(GetAll), new { salonId = body.SalonId }, result);
    }

    /// <summary>Updates a service.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ServiceDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateServiceBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateServiceCommand
        {
            Id = id,
            Name = body.Name,
            Description = body.Description,
            DurationMinutes = body.DurationMinutes,
            Price = body.Price,
            IsActive = body.IsActive,
            BarberIds = body.BarberIds
        }, ct);

        return Ok(result);
    }

    /// <summary>Deactivates a service.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteServiceCommand(id), ct);
        return NoContent();
    }

    public record CreateServiceBody(Guid SalonId, string Name, string? Description, int DurationMinutes, decimal Price);
    public record UpdateServiceBody(string Name, string? Description, int DurationMinutes, decimal Price, bool IsActive, List<Guid>? BarberIds);
}
