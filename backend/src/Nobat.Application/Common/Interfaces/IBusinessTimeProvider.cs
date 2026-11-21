namespace Nobat.Application.Common.Interfaces;

/// <summary>Business clock and timezone, so nothing calls DateTime.UtcNow directly.</summary>
public interface IBusinessTimeProvider
{
    DateTime UtcNow { get; }
    TimeZoneInfo TimeZone { get; }
    DateOnly Today { get; }
}
