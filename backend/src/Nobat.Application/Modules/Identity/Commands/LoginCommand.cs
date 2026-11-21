using MediatR;
using Nobat.Application.Modules.Identity.DTOs;

namespace Nobat.Application.Modules.Identity.Commands;

public record LoginCommand : IRequest<LoginResponse>
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}
