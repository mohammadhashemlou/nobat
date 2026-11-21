namespace Nobat.Application.Modules.Salons.DTOs;

public record SalonDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Description { get; init; }
    public string DefaultTimezone { get; init; } = "Asia/Tehran";
}

/// <summary>
/// Full public salon page payload: salon info + services + barbers.
/// </summary>
public record PublicSalonDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Address { get; init; }
    public string? Phone { get; init; }
    public string? Description { get; init; }
    public List<PublicServiceDto> Services { get; init; } = [];
    public List<PublicBarberDto> Barbers { get; init; } = [];
}

public record PublicServiceDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public int DurationMinutes { get; init; }
    public decimal Price { get; init; }
}

public record PublicBarberDto
{
    public Guid Id { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string? Bio { get; init; }
    public string? AvatarUrl { get; init; }
    public List<Guid> ServiceIds { get; init; } = [];
}
