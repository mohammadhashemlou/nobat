using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nobat.Application.Modules.Salons.Commands;
using Nobat.Application.Modules.Salons.DTOs;
using Nobat.Application.Modules.Salons.Queries;

namespace Nobat.Api.Controllers;

[ApiController]
[Route("api/v1/salons")]
[Produces("application/json")]
public class SalonsController : ControllerBase
{
    private readonly IMediator _mediator;

    public SalonsController(IMediator mediator) => _mediator = mediator;

    /// <summary>صفحه عمومی سالن (خدمات، قیمت‌ها، آرایشگران)</summary>
    [HttpGet("{slug}")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PublicSalonDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPublic(string slug, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetPublicSalonQuery(slug), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>دریافت اطلاعات سالن برای پنل مدیریت</summary>
    [HttpGet("{id:guid}")]
    [Authorize(Roles = "Admin,Barber")]
    [ProducesResponseType(typeof(SalonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _mediator.Send(new GetSalonQuery(id), ct);
        return result is null ? NotFound() : Ok(result);
    }

    /// <summary>ویرایش اطلاعات سالن</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(SalonDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSalonBody body, CancellationToken ct)
    {
        var result = await _mediator.Send(new UpdateSalonCommand
        {
            Id = id,
            Name = body.Name,
            Address = body.Address,
            Phone = body.Phone,
            Description = body.Description
        }, ct);

        return Ok(result);
    }

    public record UpdateSalonBody(string Name, string? Address, string? Phone, string? Description);
}
