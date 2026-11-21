using Nobat.Domain.Common;
using Nobat.Domain.Enums;

namespace Nobat.Domain.Entities;

public class AppointmentStatusHistory : BaseEntity
{
    public Guid AppointmentId { get; set; }
    public AppointmentStatus Status { get; set; }
    public Guid ChangedByUserId { get; set; }

    // Navigation properties
    public Appointment Appointment { get; set; } = null!;
}
