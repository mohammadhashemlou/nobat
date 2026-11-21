namespace Nobat.Application.Common.Exceptions;

/// <summary>Field-level validation failures, rendered as an RFC 7807 response.</summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("یک یا چند خطای اعتبارسنجی رخ داده است")
    {
        Errors = errors;
    }
}
