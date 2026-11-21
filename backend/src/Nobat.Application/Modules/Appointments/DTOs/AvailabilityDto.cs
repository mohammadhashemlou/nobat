namespace Nobat.Application.Modules.Appointments.DTOs;

public record TimeSlotDto
{
    /// <summary>UTC start of the slot.</summary>
    public DateTime StartUtc { get; init; }
    /// <summary>UTC end of the slot.</summary>
    public DateTime EndUtc { get; init; }
    /// <summary>Local (Tehran) start time, HH:mm.</summary>
    public string LocalStart { get; init; } = string.Empty;
    /// <summary>Local (Tehran) end time, HH:mm.</summary>
    public string LocalEnd { get; init; } = string.Empty;
}

public record AvailabilityDto
{
    public Guid BarberId { get; init; }
    public Guid ServiceId { get; init; }
    public DateOnly Date { get; init; }
    public bool IsHoliday { get; init; }
    public string? HolidayDescription { get; init; }
    public bool IsWorkingDay { get; init; }
    public List<TimeSlotDto> Slots { get; init; } = [];
}
