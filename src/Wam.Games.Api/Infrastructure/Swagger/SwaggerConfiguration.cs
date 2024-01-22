namespace Wam.Games.Api.Infrastructure.Swagger;

public static class SwaggerConfiguration
{
    /// <summary>
    /// Add swagger
    /// </summary>
    /// <param name="services">The services collection.</param>
    /// <param name="swaggerTitle">The title for the API.</param>
    /// <param name="swaggerDescription">The (optional) description for the API.</param>
    /// <param name="enableSwagger">Whether to enable swagger. Should be false for production.</param>
    /// <returns>The service collection</returns>
    public static IServiceCollection AddSwagger(this IServiceCollection services, string swaggerTitle, string? swaggerDescription = null, bool enableSwagger = true)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (string.IsNullOrWhiteSpace(swaggerTitle))
        {
            throw new ArgumentException("Please provide a title for this service.", nameof(swaggerTitle));
        }

        if (enableSwagger)
        {
            services.AddSwaggerGen();
            services.AddSingleton(new SwaggerApiOptions { Title = swaggerTitle, Description = swaggerDescription });
            services.ConfigureOptions<ConfigureSwaggerGenOptions>();
            services.ConfigureOptions<ConfigureSwaggerUIOptions>();
            services.ConfigureOptions<ConfigureSwaggerOptions>();
        }

        return services;
    }
}
