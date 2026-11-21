using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Nobat.Application.Common.Exceptions;
using Nobat.Domain.Common;

namespace Nobat.Api.Middleware;

/// <summary>
/// Centralized exception handling middleware that converts exceptions
/// into RFC 7807 ProblemDetails responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        // Structured validation errors (RFC 7807 with an `errors` map)
        if (exception is ValidationException validationException)
        {
            var validationProblem = new ValidationProblemDetails(validationException.Errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "خطای اعتبارسنجی",
                Type = "https://httpstatuses.io/400",
                Instance = context.Request.Path
            };
            validationProblem.Extensions["code"] = "VALIDATION_ERROR";
            validationProblem.Extensions["traceId"] = context.TraceIdentifier;

            _logger.LogWarning("Validation failed: {Errors}",
                string.Join(" | ", validationException.Errors.SelectMany(e => e.Value)));

            context.Response.ContentType = "application/problem+json";
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsync(JsonSerializer.Serialize(validationProblem, JsonOptions));
            return;
        }

        var (statusCode, title, code) = exception switch
        {
            DomainException domainEx => (
                MapDomainCode(domainEx.Code),
                domainEx.Message,
                domainEx.Code),
            UnauthorizedAccessException => (
                HttpStatusCode.Unauthorized,
                "دسترسی غیرمجاز",
                "UNAUTHORIZED"),
            KeyNotFoundException => (
                HttpStatusCode.NotFound,
                "موردی یافت نشد",
                "NOT_FOUND"),
            ArgumentException argEx => (
                HttpStatusCode.BadRequest,
                argEx.Message,
                "BAD_REQUEST"),
            _ => (
                HttpStatusCode.InternalServerError,
                "خطای داخلی سرور",
                "INTERNAL_ERROR")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            _logger.LogError(exception, "Unhandled exception occurred: {Message}", exception.Message);
        }
        else
        {
            _logger.LogWarning(exception, "Handled exception: {Code} - {Message}", code, exception.Message);
        }

        var problem = new ProblemDetails
        {
            Status = (int)statusCode,
            Title = title,
            Type = $"https://httpstatuses.io/{(int)statusCode}",
            Instance = context.Request.Path
        };

        problem.Extensions["code"] = code;
        problem.Extensions["traceId"] = context.TraceIdentifier;

        if (_environment.IsDevelopment() && statusCode == HttpStatusCode.InternalServerError)
        {
            problem.Extensions["detail"] = exception.ToString();
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }

    private static HttpStatusCode MapDomainCode(string code) => code switch
    {
        "EMAIL_EXISTS" => HttpStatusCode.Conflict,
        "INVALID_CREDENTIALS" => HttpStatusCode.Unauthorized,
        "INVALID_REFRESH_TOKEN" => HttpStatusCode.Unauthorized,
        "VALIDATION_ERROR" => HttpStatusCode.BadRequest,
        "USER_NOT_FOUND" => HttpStatusCode.NotFound,
        "NOT_FOUND" => HttpStatusCode.NotFound,
        "SLOT_ALREADY_BOOKED" => HttpStatusCode.Conflict,
        "FORBIDDEN" => HttpStatusCode.Forbidden,
        _ => HttpStatusCode.BadRequest
    };
}
