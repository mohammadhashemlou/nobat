namespace Nobat.Application.Common.Interfaces;

/// <summary>
/// Provides business-time context (Tehran) so application logic
/// never calls DateTime.UtcNow / TimeZoneInfo directly.
/// </summary>
public interface IBusinessTimeProvider
{
    DateTime UtcNow { get; }
    TimeZoneInfo TimeZone { get; }
    DateOnly Today { get; }
}
