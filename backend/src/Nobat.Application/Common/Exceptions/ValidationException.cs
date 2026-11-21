namespace Nobat.Application.Common.Exceptions;

/// <summary>
/// Thrown by the validation pipeline behavior. Carries field-level errors
/// so the API can emit a structured RFC 7807 response.
/// </summary>
public class ValidationException : Exception
{
    public IDictionary<string, string[]> Errors { get; }

    public ValidationException(IDictionary<string, string[]> errors)
        : base("یک یا چند خطای اعتبارسنجی رخ داده است")
    {
        Errors = errors;
    }
}
