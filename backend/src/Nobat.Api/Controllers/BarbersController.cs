using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nobat.Application.Modules.Barbers.Commands;
using Nobat.Application.Modules.Barbers.DTOs;
using Nobat.Application.Modules.Barbers.Queries;
using Nobat.Application.Modules.Schedules.Commands;
using Nobat.Application.Modules.Schedules.DTOs;
using Nobat.Application.Modules.Schedules.Queries;

namespace Nobat.Api.Controllers;

[ApiController]
[Route("api/v1/barbers")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class BarbersController : ControllerBase
{
    private readonly IMediator _mediator;

    public BarbersController(IMediator mediator) => _mediator = mediator;

    /// <summary>Barbers of a salon.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<BarberDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid salonId, [FromQuery] bool includeInactive = false, CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetBarbersQuery(salonId, includeInactive), ct);
        return Ok(result);
    }

    /// <summary>Creates a barber together with their account.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(BarberDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateBarberCommand command, CancellationToken ct)
    {
        var result = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetAll), new { salonId = command.SalonId }, result);
    }

    /// <summary>Updates a barber.</summary>
    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BarberDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBarberBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateBarberCommand
        {
            Id = id,
            FullName = body.FullName,
            Bio = body.Bio,
            AvatarUrl = body.AvatarUrl,
            IsActive = body.IsActive,
            ServiceIds = body.ServiceIds
        }, ct);

        return Ok(result);
    }

    // ---------------- Working hours ----------------

    /// <summary>Working hours of a barber.</summary>
    [HttpGet("{id:guid}/working-hours")]
    [ProducesResponseType(typeof(List<WorkingHoursDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkingHours(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetWorkingHoursQuery(id), ct);
        return Ok(result);
    }

    /// <summary>Replaces a barber's working hours.</summary>
    [HttpPut("{id:guid}/working-hours")]
    [ProducesResponseType(typeof(List<WorkingHoursDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetWorkingHours(Guid id, [FromBody] SetWorkingHoursBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new SetWorkingHoursCommand
        {
            BarberId = id,
            Items = body.Items
        }, ct);

        return Ok(result);
    }

    public record UpdateBarberBody(string FullName, string? Bio, string? AvatarUrl, bool IsActive, List<Guid>? ServiceIds);
    public record SetWorkingHoursBody(List<WorkingHoursItem> Items);
}
