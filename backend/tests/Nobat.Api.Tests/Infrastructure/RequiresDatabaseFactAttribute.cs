namespace Nobat.Api.Tests.Infrastructure;

/// <summary>
/// A <see cref="FactAttribute"/> that skips itself (with an actionable reason)
/// when no PostgreSQL server is reachable, instead of failing the whole suite.
/// </summary>
public sealed class RequiresDatabaseFactAttribute : FactAttribute
{
    public RequiresDatabaseFactAttribute()
    {
        if (!TestDatabase.IsAvailable)
            Skip = TestDatabase.UnavailableReason;
    }
}

/// <summary>Theory counterpart of <see cref="RequiresDatabaseFactAttribute"/>.</summary>
public sealed class RequiresDatabaseTheoryAttribute : TheoryAttribute
{
    public RequiresDatabaseTheoryAttribute()
    {
        if (!TestDatabase.IsAvailable)
            Skip = TestDatabase.UnavailableReason;
    }
}
