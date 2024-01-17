using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace Wam.Api.Infrastructure.Swagger;

public class ConfigureSwaggerUIOptions : IConfigureNamedOptions<SwaggerUIOptions>
{
    private readonly AuthenticationSettings _authenticationSettings;

    public ConfigureSwaggerUIOptions(IOptions<AuthenticationSettings> authenticationSettings)
    {
        _authenticationSettings = authenticationSettings.Value;
    }

    public void Configure(string name, SwaggerUIOptions options)
    {
        if (_authenticationSettings.ClientId != Guid.Empty)
        {
            options.OAuthClientId(_authenticationSettings.ClientId.ToString("D"));
        }
    }

    public void Configure(SwaggerUIOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}
