using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Appointments.Commands;
using Nobat.Application.Modules.Appointments.DTOs;
using Nobat.Application.Modules.Appointments.Queries;
using Nobat.Domain.Enums;

namespace Nobat.Api.Controllers;

[ApiController]
[Route("api/v1/appointments")]
[Produces("application/json")]
public class AppointmentsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public AppointmentsController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>محاسبه زمان‌های آزاد یک آرایشگر برای خدمت و تاریخ مشخص</summary>
    [HttpGet("availability")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAvailability(
        [FromQuery] Guid barberId,
        [FromQuery] Guid serviceId,
        [FromQuery] DateOnly date,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetAvailabilityQuery(barberId, serviceId, date), ct);
        return Ok(result);
    }

    /// <summary>لیست نوبت‌ها (بر اساس نقش فیلتر می‌شود)</summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(List<AppointmentDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? salonId,
        [FromQuery] Guid? barberId,
        [FromQuery] DateTime? fromUtc,
        [FromQuery] DateTime? toUtc,
        [FromQuery] AppointmentStatus? status,
        [FromQuery] bool upcomingOnly = false,
        CancellationToken ct = default)
    {
        Guid? customerId = null;

        // Customers can only see their own appointments
        if (_currentUser.Role == UserRole.Customer)
            customerId = _currentUser.UserId;

        var result = await _mediator.Send(new GetAppointmentsQuery
        {
            SalonId = salonId,
            BarberId = barberId,
            CustomerId = customerId,
            FromUtc = fromUtc,
            ToUtc = toUtc,
            Status = status,
            UpcomingOnly = upcomingOnly
        }, ct);

        return Ok(result);
    }

    /// <summary>ثبت نوبت جدید</summary>
    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new CreateAppointmentCommand
        {
            BarberId = body.BarberId,
            ServiceId = body.ServiceId,
            StartAtUtc = body.StartAtUtc,
            Notes = body.Notes,
            CustomerId = body.CustomerId
        }, ct);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>تغییر وضعیت نوبت (تأیید، تکمیل، عدم حضور)</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = "Admin,Barber")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusBody body, CancellationToken ct)
    {
        await _mediator.Send(new UpdateAppointmentStatusCommand(id, body.Status), ct);
        return NoContent();
    }

    /// <summary>لغو نوبت</summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        await _mediator.Send(new CancelAppointmentCommand(id), ct);
        return NoContent();
    }

    public record CreateAppointmentBody(Guid BarberId, Guid ServiceId, DateTime StartAtUtc, string? Notes, Guid? CustomerId);
    public record UpdateStatusBody(AppointmentStatus Status);
}
