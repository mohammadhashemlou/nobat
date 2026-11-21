using MediatR;
using Nobat.Application.Modules.Identity.DTOs;

namespace Nobat.Application.Modules.Identity.Commands;

public record RegisterCommand : IRequest<RegisterResponse>
{
    public string FullName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public Domain.Enums.UserRole Role { get; init; } = Domain.Enums.UserRole.Customer;
}
