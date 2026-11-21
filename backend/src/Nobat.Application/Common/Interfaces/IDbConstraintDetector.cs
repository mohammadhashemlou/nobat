namespace Nobat.Application.Common.Interfaces;

/// <summary>
/// Detects database-level constraint violations without leaking
/// provider-specific exception types into the Application layer.
/// </summary>
public interface IDbConstraintDetector
{
    /// <summary>True when the exception represents an overlap/exclusion violation (double booking).</summary>
    bool IsOverlapViolation(Exception exception);
}
