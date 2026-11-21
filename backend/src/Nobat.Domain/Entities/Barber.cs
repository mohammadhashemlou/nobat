using Nobat.Domain.Common;

namespace Nobat.Domain.Entities;

public class Barber : BaseEntity
{
    public Guid UserId { get; set; }
    public Guid SalonId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? AvatarUrl { get; set; }
    public bool IsActive { get; set; } = true;

    public Salon Salon { get; set; } = null!;
    public ICollection<BarberService> BarberServices { get; set; } = [];
    public ICollection<WorkingHours> WorkingHours { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
