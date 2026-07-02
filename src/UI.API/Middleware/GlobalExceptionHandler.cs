using Domain.Contracts.API;
using Microsoft.AspNetCore.Diagnostics;

namespace UI.API.Middleware
{
    /// <summary>
    /// Global exception handler that catches all unhandled exceptions, logs them with correlation ID,
    /// and returns a consistent error response matching the application's Result pattern.
    /// </summary>
    public class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            var correlationId = httpContext.GetCorrelationId();

            _logger.LogError(
                exception,
                "Unhandled exception occurred. CorrelationId: {CorrelationId}, Path: {Path}, QueryString: {QueryString}, Method: {Method}, User: {User}",
                correlationId,
                httpContext.Request.Path,
                httpContext.Request.QueryString,
                httpContext.Request.Method,
                httpContext.User?.Identity?.Name ?? "Anonymous"
            );

            // Create error response matching the application's error format
            var errorResponse = new ErrorResponseDto(
                type: "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                title: "An error occurred while processing your request",
                status: 500,
                traceId: correlationId, // Use correlation ID instead of new GUID
                items: new List<ErrorResponseItemDto>
                {
                    new ErrorResponseItemDto(
                        type: "UnhandledException",
                        errorDesc: exception.Message
                    )
                }
            );

            // Set response properties
            httpContext.Response.StatusCode = 500;
            httpContext.Response.ContentType = "application/json";

            // Write the error response
            await httpContext.Response.WriteAsJsonAsync(errorResponse, cancellationToken);

            // Return true to indicate the exception has been handled
            return true;
        }
    }
}
