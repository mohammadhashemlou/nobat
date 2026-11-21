using Nobat.Domain.Common;

namespace Nobat.Domain.Entities;

public class Holiday : BaseEntity
{
    public Guid SalonId { get; set; }
    public DateOnly Date { get; set; }
    public string? Description { get; set; }

    // Navigation properties
    public Salon Salon { get; set; } = null!;
}
