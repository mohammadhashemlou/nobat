using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Nobat.Application.Common.Interfaces;
using Nobat.Application.Modules.Identity.Commands;
using Nobat.Application.Modules.Identity.DTOs;

namespace Nobat.Api.Controllers;

[ApiController]
[Route("api/v1/auth")]
[Produces("application/json")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ICurrentUserService _currentUser;

    public AuthController(IMediator mediator, ICurrentUserService currentUser)
    {
        _mediator = mediator;
        _currentUser = currentUser;
    }

    /// <summary>ثبت‌نام کاربر جدید (مشتری یا آرایشگر)</summary>
    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RegisterResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var command = new RegisterCommand
        {
            FullName = request.FullName,
            Email = request.Email,
            Password = request.Password,
            ConfirmPassword = request.ConfirmPassword,
            PhoneNumber = request.PhoneNumber,
            Role = request.Role
        };

        var result = await _mediator.Send(command, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>ورود و دریافت توکن دسترسی</summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new LoginCommand
        {
            Email = request.Email,
            Password = request.Password
        }, ct);

        return Ok(result);
    }

    /// <summary>تمدید توکن دسترسی با استفاده از توکن بازنشانی</summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(RefreshTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _mediator.Send(new RefreshTokenCommand
        {
            RefreshToken = request.RefreshToken
        }, ct);

        return Ok(result);
    }

    /// <summary>اطلاعات کاربر جاری</summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Me()
    {
        return Ok(new
        {
            id = _currentUser.UserId,
            email = _currentUser.Email,
            fullName = _currentUser.FullName,
            role = _currentUser.Role?.ToString()
        });
    }
}
