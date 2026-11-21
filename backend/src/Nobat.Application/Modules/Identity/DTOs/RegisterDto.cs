using Nobat.Domain.Enums;

namespace Nobat.Application.Modules.Identity.DTOs;

public record RegisterRequest
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string ConfirmPassword { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public UserRole Role { get; init; } = UserRole.Customer;
}

public record RegisterResponse
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public UserRole Role { get; init; }
}
