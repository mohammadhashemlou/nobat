namespace Nobat.Application.Modules.Services.DTOs;

public record ServiceDto
{
    public Guid Id { get; init; }
    public Guid SalonId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
    public bool IsActive { get; init; }
    public List<Guid> BarberIds { get; init; } = [];
}
