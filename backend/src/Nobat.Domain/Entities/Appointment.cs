using Nobat.Domain.Common;
using Nobat.Domain.Enums;

namespace Nobat.Domain.Entities;

public class Appointment : BaseEntity, IConcurrencyAware
{
    public Guid SalonId { get; set; }
    public Guid BarberId { get; set; }
    public Guid CustomerId { get; set; }
    public Guid ServiceId { get; set; }

    /// <summary>
    /// Appointment start time in UTC.
    /// </summary>
    public DateTime StartAt { get; set; }

    /// <summary>
    /// Appointment end time in UTC.
    /// </summary>
    public DateTime EndAt { get; set; }

    public AppointmentStatus Status { get; set; } = AppointmentStatus.Pending;
    public string? Notes { get; set; }
    public DateTime? CancelledAt { get; set; }

    /// <summary>
    /// Row version for optimistic concurrency control.
    /// </summary>
    public byte[] RowVersion { get; set; } = [];

    // Navigation properties
    public Salon Salon { get; set; } = null!;
    public Barber Barber { get; set; } = null!;
    public ApplicationUser Customer { get; set; } = null!;
    public Service Service { get; set; } = null!;
    public ICollection<AppointmentStatusHistory> StatusHistory { get; set; } = [];
}
