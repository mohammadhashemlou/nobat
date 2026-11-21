using Microsoft.EntityFrameworkCore;
using Nobat.Application.Common.Interfaces;
using Npgsql;

namespace Nobat.Infrastructure.Common.Services;

public class DbConstraintDetector : IDbConstraintDetector
{
    // PostgreSQL SQLSTATE for exclusion_violation
    private const string ExclusionViolation = "23P01";
    // unique_violation
    private const string UniqueViolation = "23505";

    public bool IsOverlapViolation(Exception exception)
    {
        var current = exception;
        while (current is not null)
        {
            if (current is PostgresException pg
                && (pg.SqlState == ExclusionViolation || pg.SqlState == UniqueViolation))
            {
                return true;
            }

            if (current is DbUpdateException dbEx && dbEx.InnerException is not null)
            {
                current = dbEx.InnerException;
                continue;
            }

            current = current.InnerException;
        }

        return false;
    }
}
