namespace Nobat.Domain.Entities;

/// <summary>
/// Junction table for M:N relationship between Barber and Service.
/// </summary>
public class BarberService
{
    public Guid BarberId { get; set; }
    public Guid ServiceId { get; set; }

    // Navigation properties
    public Barber Barber { get; set; } = null!;
    public Service Service { get; set; } = null!;
}
