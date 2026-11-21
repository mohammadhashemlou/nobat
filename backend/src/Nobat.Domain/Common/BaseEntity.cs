namespace Nobat.Domain.Common;

/// <summary>Audit fields shared by every entity. Timestamps are UTC.</summary>
public abstract class BaseEntity
{
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
