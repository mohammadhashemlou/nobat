namespace Nobat.Application.Modules.Barbers.DTOs;

public record BarberDto
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public Guid SalonId { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? PhoneNumber { get; init; }
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public bool IsActive { get; init; }
    public List<Guid> ServiceIds { get; init; } = [];
}
