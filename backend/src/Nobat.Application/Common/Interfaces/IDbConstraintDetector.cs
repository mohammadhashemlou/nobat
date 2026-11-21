namespace Nobat.Application.Common.Interfaces;

/// <summary>Recognises provider-specific constraint violations without leaking the provider.</summary>
public interface IDbConstraintDetector
{
    /// <summary>True when the exception represents an overlap/exclusion violation (double booking).</summary>
    bool IsOverlapViolation(Exception exception);
}
