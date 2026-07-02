namespace UI.API.Middleware
{
    public static class CorrelationIdHttpContextExtensions
    {
        public const string HeaderName = "X-Correlation-ID";

        /// <summary>
        /// Reads the correlation ID that <see cref="CorrelationIdMiddleware"/> stamped onto the response,
        /// falling back to <see cref="HttpContext.TraceIdentifier"/> if the middleware hasn't run yet.
        /// </summary>
        public static string GetCorrelationId(this HttpContext context) =>
            context.Response.Headers[HeaderName].FirstOrDefault() ?? context.TraceIdentifier;
    }
}
