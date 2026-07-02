using Domain.Interfaces;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Services.Infrastructure;
using Services.Infrastructure.Caching;

namespace IoC
{
    /// <summary>
    /// Central dependency injection configuration for the application.
    /// Registers all services, repositories, validators, and infrastructure components.
    /// </summary>
    public static class DIBootstrapper
    {
        /// <summary>
        /// Registers all application services and infrastructure dependencies.
        /// This method is called from Program.cs during application startup.
        /// </summary>
        /// <param name="services">The service collection to register dependencies into</param>
        /// <param name="configuration">Application configuration for reading settings</param>
        public static void RegisterCustomServices(IServiceCollection services, IConfiguration configuration)
        {
            // ============================================================================
            // CQRS & MEDIATR CONFIGURATION
            // ============================================================================
            // Register MediatR for command/query handling (CQRS pattern)
            var servicesAssembly = typeof(Services.Core.BaseCommandHandler).Assembly;
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssembly(servicesAssembly);
            });

            // Register mediator handler interface for command/query dispatching
            services.AddScoped<IMediatorHandler, MediatorHandler>();

            // Register all FluentValidation validators from Services assembly
            services.AddValidatorsFromAssembly(servicesAssembly);

            // ============================================================================
            // CACHING
            // ============================================================================
            services.AddMemoryCache();
            services.AddSingleton<ICacheService, MemoryCacheService>();
        }
    }
}
