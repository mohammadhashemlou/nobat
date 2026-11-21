namespace Nobat.Domain.Common;

/// <summary>
/// Base class for all entities with audit fields.
/// Timestamps are stored in UTC.
/// </summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
