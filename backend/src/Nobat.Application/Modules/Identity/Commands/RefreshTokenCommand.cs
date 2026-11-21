using MediatR;
using Nobat.Application.Modules.Identity.DTOs;

namespace Nobat.Application.Modules.Identity.Commands;

public record RefreshTokenCommand : IRequest<RefreshTokenResponse>
{
    public string RefreshToken { get; init; } = string.Empty;
}
