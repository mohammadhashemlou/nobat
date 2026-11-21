using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nobat.Application.Modules.Schedules.Commands;
using Nobat.Application.Modules.Schedules.DTOs;
using Nobat.Application.Modules.Schedules.Queries;

namespace Nobat.Api.Controllers;

[ApiController]
[Route("api/v1/holidays")]
[Produces("application/json")]
[Authorize(Roles = "Admin")]
public class HolidaysController : ControllerBase
{
    private readonly IMediator _mediator;

    public HolidaysController(IMediator mediator) => _mediator = mediator;

    /// <summary>Salon holidays.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(List<HolidayDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] Guid salonId, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetHolidaysQuery(salonId), ct);
        return Ok(result);
    }

    /// <summary>Adds a holiday.</summary>
    [HttpPost]
    [ProducesResponseType(typeof(HolidayDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] AddHolidayBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new AddHolidayCommand
        {
            SalonId = body.SalonId,
            Date = body.Date,
            Description = body.Description
        }, ct);

        return CreatedAtAction(nameof(GetAll), new { salonId = body.SalonId }, result);
    }

    /// <summary>Removes a holiday.</summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteHolidayCommand(id), ct);
        return NoContent();
    }

    public record AddHolidayBody(Guid SalonId, DateOnly Date, string? Description);
}
