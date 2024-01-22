using System.Reflection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Wam.Games.Api.Infrastructure.Swagger;

internal class ConfigureSwaggerGenOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly AuthenticationSettings _authenticationSettings;
    private readonly SwaggerApiOptions _apiOptions;

    public ConfigureSwaggerGenOptions(IOptions<AuthenticationSettings> authenticationSettings, SwaggerApiOptions apiOptions)
    {
        _authenticationSettings = authenticationSettings.Value;
        _apiOptions = apiOptions;
    }

    public void Configure(string name, SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo { Title = _apiOptions.Title, Description = _apiOptions.Description, Version = "v1" });

        options.DocumentFilter<SwaggerDocumentHealthChecks>();

        options.ResolveConflictingActions(apiDescriptions => apiDescriptions.Last());

        // Set the comments path for the Swagger JSON and UI.
        var xmlFile = $"{Assembly.GetEntryAssembly()?.GetName()?.Name ?? "None"}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        if (_authenticationSettings.ClientId != Guid.Empty)
        {
            AddAadSecurity(options);
        }
    }

    private void AddAadSecurity(SwaggerGenOptions options)
    {
        var scope = string.IsNullOrWhiteSpace(_authenticationSettings.ApiScope)
            ? _authenticationSettings.DefaultScope
            : _authenticationSettings.ApiScope;

        options.AddSecurityDefinition("oauth2",
            new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        TokenUrl =
                            new Uri(
                                $"{_authenticationSettings.Instance}/{_authenticationSettings.TenantId}/oauth2/v2.0/token"),
                        AuthorizationUrl =
                            new Uri(
                                $"{_authenticationSettings.Instance}/{_authenticationSettings.TenantId}/oauth2/v2.0/authorize"),
                        Scopes = {{ scope, "Default" }}
                    }
                }
            });

        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme, Id = "oauth2"
                    }
                },
                new string[] { }
            }
        });
    }

    private static void AddApiKeySecurity(SwaggerGenOptions options, string headerName)
    {
        options.AddSecurityDefinition("APIKey",
            new OpenApiSecurityScheme
            {
                Description = "API key must appear in header",
                Type = SecuritySchemeType.ApiKey,
                Name = headerName,
                In = ParameterLocation.Header
            });
        options.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "APIKey"
                    }
                },
                new string[] { }
            }
        });
    }

    public void Configure(SwaggerGenOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}
