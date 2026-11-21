using Nobat.Domain.Common;

namespace Nobat.Domain.Entities;

public class Salon : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Description { get; set; }
    public string DefaultTimezone { get; set; } = "Asia/Tehran";

    // Navigation properties
    public ICollection<Barber> Barbers { get; set; } = [];
    public ICollection<Service> Services { get; set; } = [];
    public ICollection<Holiday> Holidays { get; set; } = [];
    public ICollection<Appointment> Appointments { get; set; } = [];
}
