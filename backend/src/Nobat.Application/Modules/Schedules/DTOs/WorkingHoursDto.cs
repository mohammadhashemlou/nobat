namespace Nobat.Application.Modules.Schedules.DTOs;

public record WorkingHoursDto
{
    public Guid Id { get; init; }
    public Guid BarberId { get; init; }
    public string DayOfWeek { get; init; } = string.Empty;
    public int DayOfWeekValue { get; init; }
    public string StartTime { get; init; } = string.Empty;
    public string EndTime { get; init; } = string.Empty;
    public string? BreakStart { get; init; }
    public string? BreakEnd { get; init; }
}

public record HolidayDto
{
    public Guid Id { get; init; }
    public DateOnly Date { get; init; }
    public string? Description { get; init; }
}
