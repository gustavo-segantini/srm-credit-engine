using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SrmCreditEngine.Domain.Exceptions;

namespace SrmCreditEngine.API.Middleware;

/// <summary>
/// Global exception handler middleware.
/// Translates domain exceptions and unhandled errors to RFC 7807 Problem Details responses.
/// </summary>
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
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
        var (statusCode, code, message) = exception switch
        {
            // Duplicate document conflicts map to 409; all other business rule violations map to 422
            BusinessRuleViolationException { Code: "DUPLICATE_SETTLEMENT" } e => (HttpStatusCode.Conflict, e.Code, e.Message),
            BusinessRuleViolationException e => (HttpStatusCode.UnprocessableEntity, e.Code, e.Message),
            ExchangeRateNotFoundException e => (HttpStatusCode.NotFound, e.Code, e.Message),
            InvalidPricingException e => (HttpStatusCode.BadRequest, e.Code, e.Message),
            DomainException e => (HttpStatusCode.BadRequest, e.Code, e.Message),
            DbUpdateConcurrencyException => (
                HttpStatusCode.Conflict,
                "CONCURRENCY_CONFLICT",
                "The resource was modified by another request. Please retry."),
            OperationCanceledException => (
                HttpStatusCode.BadRequest,
                "REQUEST_CANCELLED",
                "The request was cancelled."),
            _ => (
                HttpStatusCode.InternalServerError,
                "INTERNAL_ERROR",
                "An unexpected error occurred. Please try again later.")
        };

        if (statusCode == HttpStatusCode.InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", 
                context.Request.Method, context.Request.Path);
        else
            _logger.LogWarning(exception, "Domain exception: {Code} for {Method} {Path}",
                code, context.Request.Method, context.Request.Path);

        context.Response.StatusCode = (int)statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new
        {
            type = $"https://srm-credit-engine.dev/errors/{code.ToLowerInvariant().Replace('_', '-')}",
            title = code,
            status = (int)statusCode,
            detail = message,
            instance = context.Request.Path.Value,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(
            JsonSerializer.Serialize(problemDetails, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            }));
    }
}
