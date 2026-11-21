using Nobat.Domain.Common;

namespace Nobat.Domain.Entities;

public class WorkingHours : BaseEntity
{
    public Guid BarberId { get; set; }
    public Enums.DayOfWeek DayOfWeek { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }
    public TimeOnly? BreakStart { get; set; }
    public TimeOnly? BreakEnd { get; set; }

    // Navigation properties
    public Barber Barber { get; set; } = null!;
}
