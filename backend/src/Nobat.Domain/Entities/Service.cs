using Nobat.Domain.Common;

namespace Nobat.Domain.Entities;

public class Service : BaseEntity
{
    public Guid SalonId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public decimal Price { get; set; }
    public bool IsActive { get; set; } = true;

    // Navigation properties
    public Salon Salon { get; set; } = null!;
    public ICollection<BarberService> BarberServices { get; set; } = [];
}
