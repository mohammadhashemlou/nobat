namespace Nobat.Application.Modules.Identity.DTOs;

public record LoginRequest
{
    public string Email { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; init; }
    public DateTime RefreshTokenExpiresAt { get; init; }
    public UserInfo User { get; init; } = new();
}

public record UserInfo
{
    public Guid Id { get; init; }
    public string Email { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
}
