using IoC;
using Microsoft.AspNetCore.HttpOverrides;
using UI.API.Configurations;
using UI.API.Middleware;

namespace UI.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // ============================================================================
            // CONFIGURATION SOURCES
            // ============================================================================
            // Load configuration from multiple sources in priority order:
            // 1. appsettings.json (base configuration)
            // 2. appsettings.{Environment}.json (environment-specific overrides)
            // 3. Environment variables (deployment overrides)
            builder.Configuration
                   .SetBasePath(Directory.GetCurrentDirectory())
                   .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                   .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
                   .AddEnvironmentVariables();

            // ============================================================================
            // DEPENDENCY INJECTION
            // ============================================================================
            // Business-layer registrations (MediatR, validators, caching) live in the IoC composition
            // root; host/infrastructure registrations (HttpClient, resilience) stay here since IoC
            // can't reference UI.API back.
            DIBootstrapper.RegisterCustomServices(builder.Services, builder.Configuration);
            builder.Services.AddResiliencePolicies();
            builder.Services.AddHackerNewsClient(builder.Configuration);

            // ============================================================================
            // CORS (Cross-Origin Resource Sharing)
            // ============================================================================
            // Configure CORS to allow frontend applications to access the API
            // Allowed origins are configured per environment in appsettings.json
            var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
                ?? new[] { "http://localhost:4200" };

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.WithOrigins(allowedOrigins)
                              .AllowAnyMethod()
                              .AllowAnyHeader()
                              .AllowCredentials();
                    });
            });

            // Configure forwarded headers for reverse proxy support (Azure, nginx, etc.)
            builder.Services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
                options.KnownProxies.Clear();
                options.KnownNetworks.Clear();
            });

            // Configure controllers with JSON serialization options
            builder.Services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never;
            });

            // Configure API documentation (Swagger/OpenAPI)
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerConfig();

            // Configure global exception handling
            builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
            builder.Services.AddProblemDetails();

            // ============================================================================
            // BUILD APPLICATION
            // ============================================================================
            var app = builder.Build();

            // ============================================================================
            // MIDDLEWARE PIPELINE
            // ============================================================================
            // Configure HTTP request pipeline (ORDER MATTERS!)
            // Middleware executes in the order it's added

            // 1. Forwarded headers (must be first for reverse proxy scenarios)
            app.UseForwardedHeaders();

            // 2. Correlation ID (for request tracing across services)
            app.UseMiddleware<CorrelationIdMiddleware>();

            // 3. Exception handling (catch unhandled exceptions)
            app.UseExceptionHandler(options => { });

            // 4. Swagger UI (development only)
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            // 5. CORS (must be before Authentication/Authorization)
            app.UseCors("AllowFrontend");

            // 6. Map controllers (route requests to endpoints)
            app.MapControllers();

            // ============================================================================
            // START APPLICATION
            // ============================================================================
            app.Run();
        }
    }
}
