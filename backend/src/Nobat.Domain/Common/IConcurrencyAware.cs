namespace Nobat.Domain.Common;

/// <summary>
/// Interface for entities that support optimistic concurrency via row version.
/// </summary>
public interface IConcurrencyAware
{
    /// <summary>
    /// Row version for optimistic concurrency control.
    /// </summary>
    byte[] RowVersion { get; set; }
}
