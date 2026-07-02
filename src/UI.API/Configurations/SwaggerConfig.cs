using Microsoft.OpenApi.Models;
using System.Reflection;

namespace UI.API.Configurations
{
    public static class SwaggerConfig
    {
        public static void AddSwaggerConfig(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1",
                    new OpenApiInfo
                    {
                        Title = "Santander API",
                        Version = "v1",
                        Description = "Returns the best n Hacker News stories by score, sourced live from the public Hacker News API.",
                        Contact = new OpenApiContact
                        {
                            Name = "Daniel Gregatto",
                            Email = "daniel.gregatto@gmail.com"
                        }
                    });

                // Include XML comments
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                c.IncludeXmlComments(xmlPath, includeControllerXmlComments: true);
            });
        }
    }
}
